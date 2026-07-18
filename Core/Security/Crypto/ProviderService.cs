using OptimusPrime.Features.Contracts.Commands;
using System.Security.Cryptography;
using System.Text;

namespace OptimusPrime.Core.Security.Crypto;

public class ProviderService(
    EcdhProvider ecdhProvider,
    EkeProvider ekeProvider,
    ILogger<ProviderService> logger) {

    private readonly EcdhProvider _ecdhProvider = ecdhProvider;
    private readonly EkeProvider _ekeProvider = ekeProvider;
    private readonly ILogger<ProviderService> _logger = logger;

    // ==============================================================================
    // Client Request: generates the payload to send to the server
    // ==============================================================================
    public (RegisterCommand Command, byte[] TempPrivateKey) ClientRequest(RegisterCommand command) {

        // generate the client's temporary ECDH key pair 
        using var tempEcdhPair = _ecdhProvider.GenerateTunnelKeyPair();

        // encrypt the temporary public key 
        var tempPublicKeyRaw = _ecdhProvider.ExportUncompressedPublicKey(tempEcdhPair);
        var encryptedEke = _ekeProvider.EncryptWithPassword(tempPublicKeyRaw);

        var tempPrivateKey = tempEcdhPair.ExportECPrivateKey();
        var user = new RegisterCommand {
            Email = command.Email,
            Password = command.Password,
            EncryptedEPub = Convert.ToBase64String(encryptedEke.Ciphertext),
            Nonce = Convert.ToBase64String(encryptedEke.Nonce),
            Tag = Convert.ToBase64String(encryptedEke.Tag),
            Salt = Convert.ToBase64String(encryptedEke.Salt!)
        };

        return (user, tempPrivateKey);
    }

    // ==============================================================================
    // Server Execution: generates the definitive key and the blob
    // ==============================================================================
    public (string Payload, string PublicKey)? ServerExecute(RegisterCommand request) {

        try {
            // decrypt the client's temporary public key 
            var ciphertext = Convert.FromBase64String(request.EncryptedEPub);
            var nonce = Convert.FromBase64String(request.Nonce);
            var tag = Convert.FromBase64String(request.Tag);
            var salt = Convert.FromBase64String(request.Salt);
            byte[] clientTempPublicKeyRaw = _ekeProvider.DecryptWithPassword(ciphertext, nonce, tag, salt);

            // generate the server's temporary ECDH key pair
            using var serverTempEcdhPair = _ecdhProvider.GenerateTunnelKeyPair();

            // calculate the shared secret 
            byte[]? sharedSecret = _ecdhProvider.DeriveSharedSecret(serverTempEcdhPair, clientTempPublicKeyRaw);
            if(sharedSecret == null) {
                _logger.LogError("Invalid remote public key format. Expected exactly 65 bytes starting with 0x04.");
                return null;
            }

            // generate the definitive device identity: ECDSA key pair
            using var definitiveDeviceKeyPair = _ecdhProvider.GenerateTunnelKeyPair();

            // wrapper 1: encrypt the server's temporary public key using the EKE password
            var serverTempPublicKeyRaw = _ecdhProvider.ExportUncompressedPublicKey(serverTempEcdhPair);
            var wrapper1 = _ekeProvider.EncryptWithPassword(serverTempPublicKeyRaw);

            // wrapper 2: encrypt the definitive private key using a double layer (password + shared secret)
            byte[] definitivePrivateKeyRaw = definitiveDeviceKeyPair.ExportECPrivateKey();
            var wrapper2 = _ekeProvider.EncryptDoubleLayer(definitivePrivateKeyRaw, sharedSecret);

            // assemble the pieces into a single plain string separated by colons ':'
            string plainBlob =
                $"{Convert.ToBase64String(wrapper1.Ciphertext)}:" +
                $"{Convert.ToBase64String(wrapper1.Nonce)}:" +
                $"{Convert.ToBase64String(wrapper1.Tag)}:" +
                $"{Convert.ToBase64String(wrapper1.Salt!)}:" +
                $"{Convert.ToBase64String(wrapper2.Ciphertext)}:" +
                $"{Convert.ToBase64String(wrapper2.Nonce)}:" +
                $"{Convert.ToBase64String(wrapper2.Tag)}:" +
                $"{Convert.ToBase64String(wrapper2.Salt!)}";

            // wrapper 3: seal the entire string and extract the definitive public key to store it in the database
            string payload = _ekeProvider.PackServerBlob(plainBlob);
            string definitivePublicKey = Convert.ToBase64String(_ecdhProvider.ExportUncompressedPublicKey(definitiveDeviceKeyPair));

            return (
                Payload: payload,
                PublicKey: definitivePublicKey
            );

        } catch(Exception ex) {
            _logger.LogError("Provisioning failed: {Message}", ex.Message);
            return null;
        }
    }

    // ==============================================================================
    // Client Response: unpack the blob and extracts the definitive key
    // ==============================================================================
    public byte[]? ClientResponse(string payloadBase64, byte[] tempPrivateKey) {

        try {
            // reconstruct the client's temporary ECDH object from the saved private key
            using var localTempEcdh = ECDiffieHellman.Create();
            localTempEcdh.ImportECPrivateKey(tempPrivateKey, out _);

            // unpack the outer layer (wrapper 3) using the Server Master Key
            var parts = _ekeProvider.UnpackServerBlob(payloadBase64);
            if(parts == null) {
                _logger.LogError("Malformed blob: expected 8 segments.");
                return null;
            }

            // unpack the server's temporary public key (wrapper 1) using the EKE password
            var c1 = Convert.FromBase64String(parts[0]);
            var n1 = Convert.FromBase64String(parts[1]);
            var t1 = Convert.FromBase64String(parts[2]);
            var s1 = Convert.FromBase64String(parts[3]);
            var serverTempPubKeyRaw = _ekeProvider.DecryptWithPassword(c1, n1, t1, s1);

            // calculate the shared secret combining client's private and server's public temporary keys
            var sharedSecret = _ecdhProvider.DeriveSharedSecret(localTempEcdh, serverTempPubKeyRaw);
            if(sharedSecret == null) {
                _logger.LogError("Invalid remote public key format. Expected exactly 65 bytes starting with 0x04.");
                return null;
            }

            // remove the double layer (wrapper 2) to reveal the definitive private key
            var c2 = Convert.FromBase64String(parts[4]);
            var n2 = Convert.FromBase64String(parts[5]);
            var t2 = Convert.FromBase64String(parts[6]);
            var s2 = Convert.FromBase64String(parts[7]);

            byte[] definitivePrivateKey = _ekeProvider.DecryptDoubleLayer(c2, n2, t2, s2, sharedSecret);

            return definitivePrivateKey;

        } catch(Exception ex) {
            _logger.LogError("Unpacking failed: {Message}", ex.Message);
            return null;
        }
    }

    // ==========================
    // Sign timestamp
    // ==========================
    public string SignPayload(string payloadToSign, byte[] privateKey) {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportECPrivateKey(privateKey, out _);

        byte[] dataToSign = Encoding.UTF8.GetBytes(payloadToSign);
        byte[] signature = ecdsa.SignData(
            dataToSign,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation
        );

        return Convert.ToBase64String(signature);
    }

    // ==========================
    // Verify signature
    // ==========================
    public bool VerifySignature(string payload, string signatureBase64, string publicKeyBase64) {
        try {
            byte[] publicKeyRaw = Convert.FromBase64String(publicKeyBase64);
            byte[] signature = Convert.FromBase64String(signatureBase64);
            byte[] dataToVerify = Encoding.UTF8.GetBytes(payload);

            var parameters = new ECParameters {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint {
                    X = publicKeyRaw[1..33],
                    Y = publicKeyRaw[33..65]
                }
            };

            using var ecdsa = ECDsa.Create(parameters);
            return ecdsa.VerifyData(
                dataToVerify,
                signature,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation
            );

        } catch {
            return false;
        }

    }
}