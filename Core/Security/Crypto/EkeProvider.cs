using Microsoft.Extensions.Options;
using OptimusPrime.Core.Settings;
using System.Security.Cryptography;
using System.Text;

namespace OptimusPrime.Core.Security.Crypto;

public class EkeProvider(IOptions<EkeSettings> eke) {

    private readonly EkeSettings _ekeSettings = eke.Value;

    // ====================================================
    // EKE Password Encryption / Decryption
    // ====================================================

    // encrypts data using AES-GCM with a key derived from the EKE password via PBKDF2
    public EkeModel EncryptWithPassword(byte[] rawData) {

        var salt = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[rawData.Length];

        // derive the AES key from the pre-shared password
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            password: Encoding.UTF8.GetBytes(_ekeSettings.Password),
            salt: salt,
            iterations: _ekeSettings.Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: _ekeSettings.KeyLength);

        using var aesGcm = new AesGcm(derivedKey, tag.Length);
        aesGcm.Encrypt(nonce, rawData, ciphertext, tag);

        return new EkeModel {
            Ciphertext = ciphertext,
            Nonce = nonce,
            Tag = tag,
            Salt = salt
        };
    }

    // decrypts data using AES-GCM with a key derived from the EKE password and the provided salt. Verifies authenticity via tag.
    public byte[] DecryptWithPassword(byte[] ciphertext, byte[] nonce, byte[] tag, byte[] salt) {

        // derive the exact same AES key using the provided salt
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            password: Encoding.UTF8.GetBytes(_ekeSettings.Password),
            salt: salt,
            iterations: _ekeSettings.Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: _ekeSettings.KeyLength);

        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(derivedKey, tag.Length);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    // ====================================================
    // Blob Methods: server packing
    // ====================================================

    // encrypts the definitive private key with a double layer
    public EkeModel EncryptDoubleLayer(byte[] definitivePrivateKeyRaw, byte[] sharedSecret) {

        // layer 1: encrypt the private key with the EKE password
        var layer1 = EncryptWithPassword(definitivePrivateKeyRaw);

        // prepare the packet for the second layer by concatenating: Nonce (12) | Tag (16) | Ciphertext (N)
        var layer1Packet = new byte[12 + 16 + layer1.Ciphertext.Length];
        Buffer.BlockCopy(layer1.Nonce, 0, layer1Packet, 0, 12);
        Buffer.BlockCopy(layer1.Tag, 0, layer1Packet, 12, 16);
        Buffer.BlockCopy(layer1.Ciphertext, 0, layer1Packet, 28, layer1.Ciphertext.Length);

        // layer 2: encrypt the Layer 1 packet using the ECDH Shared Secret
        var (Ciphertext, Nonce, Tag) = EncryptAesGcm(sharedSecret, layer1Packet);

        return new EkeModel {
            Ciphertext = Ciphertext,
            Nonce = Nonce,
            Tag = Tag,
            Salt = layer1.Salt 
        };
    }

    // seals the assembled plain blob (wrapper 3) using the Server Master Key
    public string PackServerBlob(string plainBlob) {

        var serverKey = Convert.FromBase64String(_ekeSettings.ServerKeyB64);
        var plaintext = Encoding.UTF8.GetBytes(plainBlob);

        // encrypt the entire string with the Master Key
        var (Ciphertext, Nonce, Tag) = EncryptAesGcm(serverKey, plaintext);

        // concatenate the final output: Nonce (12) | Tag (16) | Ciphertext (N)
        var finalBlob = new byte[12 + 16 + Ciphertext.Length];
        Buffer.BlockCopy(Nonce, 0, finalBlob, 0, 12);
        Buffer.BlockCopy(Tag, 0, finalBlob, 12, 16);
        Buffer.BlockCopy(Ciphertext, 0, finalBlob, 28, Ciphertext.Length);

        return Convert.ToBase64String(finalBlob);
    }

    // ====================================================
    // Blob Methods: client unpacking
    // ====================================================

    // unpacks the outer envelope (wrapper 3) using the Server Master Key
    public string[]? UnpackServerBlob(string base64Blob) {

        var blob = Convert.FromBase64String(base64Blob);
        var serverKey = Convert.FromBase64String(_ekeSettings.ServerKeyB64);

        // the server packed it as: Nonce (12) | Tag (16) | Ciphertext (N)
        var nonce = blob[..12];
        var tag = blob[12..28];
        var ciphertext = blob[28..];

        var plaintextBytes = DecryptAesGcm(serverKey, nonce, ciphertext, tag);
        var plainString = Encoding.UTF8.GetString(plaintextBytes);

        var parts = plainString.Split(':');
        if(parts.Length != 8) return null;

        return parts;
    }

    // removes the double layer (wrapper 2) to reveal the definitive private key
    public byte[] DecryptDoubleLayer(byte[] ciphertext, byte[] nonce, byte[] tag, byte[] salt, byte[] sharedSecret) {

        // remove the outer layer: encrypted with the Shared Secret 
        var layer1Packet = DecryptAesGcm(sharedSecret, nonce, ciphertext, tag);

        // the extracted inner packet is structured as: Nonce1 (12) | Tag1 (16) | Ciphertext1 (N)
        var innerNonce = layer1Packet[..12];
        var innerTag = layer1Packet[12..28];
        var innerCiphertext = layer1Packet[28..];

        // remove the inner layer: encrypted with the EKE Password
        return DecryptWithPassword(innerCiphertext, innerNonce, innerTag, salt);
    }

    // ==========================
    //  AES-GCM Helpers
    // ==========================

    // encrypt data using AES-GCM with a given key, returning the ciphertext, nonce, and tag
    private static (byte[] Ciphertext, byte[] Nonce, byte[] Tag) EncryptAesGcm(byte[] key, byte[] plaintext) {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];

        using var aesGcm = new AesGcm(key, tag.Length);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        return (ciphertext, nonce, tag);
    }

    // decrypt data using AES-GCM with a given key, returning the plaintext bytes
    private static byte[] DecryptAesGcm(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag) {
        using var aesGcm = new AesGcm(key, tag.Length);
        var plaintext = new byte[ciphertext.Length];

        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}

// ==========================
// Eke Model
// ==========================

public class EkeModel {
    public required byte[] Ciphertext { get; set; }
    public required byte[] Nonce { get; set; }
    public required byte[] Tag { get; set; }
    public byte[]? Salt { get; set; }
}