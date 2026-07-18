using System.Security.Cryptography;
using System.Text;

namespace OptimusPrime.Core.Security.Crypto;

public class EcdhProvider {

    // ====================================================
    // ECDH Key Pair Generation & Export
    // ====================================================

    // generates an ECDH cryptographic key pair 
    public ECDiffieHellman GenerateTunnelKeyPair() => ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

    // exports the public key in the standard uncompressed format (65 bytes: 0x04 || X || Y)
    public byte[] ExportUncompressedPublicKey(ECDiffieHellman ecdh) {

        // export only the public parameters (false = exclude private key)
        var parameters = ecdh.ExportParameters(false);
        var q = parameters.Q;

        var rawUncompressedKey = new byte[65];
        rawUncompressedKey[0] = 0x04; 

        // copy the X and Y coordinates into the buffer, padding left with zeros if necessary
        CopyPadLeft(q.X!, rawUncompressedKey.AsSpan(1, 32));
        CopyPadLeft(q.Y!, rawUncompressedKey.AsSpan(33, 32));

        return rawUncompressedKey;
    }

    // ====================================================
    // Shared Secret Derivation & HKDF Expansion
    // ====================================================

    // derives a 32-byte shared secret using ECDH Key Agreement and expands it via HKDF-SHA256
    public byte[]? DeriveSharedSecret(ECDiffieHellman localEcdh, byte[] remotePublicKeyRaw) {

        // validate that the remote public key is in the correct uncompressed format and length
        if(remotePublicKeyRaw.Length != 65 || remotePublicKeyRaw[0] != 0x04) return null;

        // reconstruct the remote ECParameters from the raw uncompressed bytes
        var remoteParameters = new ECParameters {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint {
                X = remotePublicKeyRaw[1..33],
                Y = remotePublicKeyRaw[33..65]
            }
        };

        using var remoteEcdh = ECDiffieHellman.Create(remoteParameters);

        // execute the ECDH basic agreement to derive the raw secret (the X coordinate of the shared point)
        byte[] rawSecretAgreement = localEcdh.DeriveRawSecretAgreement(remoteEcdh.PublicKey);

        // apply HKDF (Extract + Expand) to safely derive a cryptographically strong 32-byte key
        return HKDF.DeriveKey(
            hashAlgorithmName: HashAlgorithmName.SHA256,
            ikm: rawSecretAgreement,
            outputLength: 32,
            salt: Array.Empty<byte>(),
            info: Encoding.UTF8.GetBytes("device-key-transport")
        );
    }

    // ==========================================
    // Helpers
    // ==========================================

    // copy a byte array into a destination span, padding with leading zeros if the source is shorter
    private void CopyPadLeft(byte[] source, Span<byte> destination) {
        var startAt = destination.Length - source.Length;
        source.CopyTo(destination[startAt..]);
    }
}