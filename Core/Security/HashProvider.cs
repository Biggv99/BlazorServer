namespace OptimusPrime.Core.Security;

public class HashProvider() {

    // ==========================
    //  Hashing and Verifying
    // ==========================
    public string Hash(string toHash) => BCrypt.Net.BCrypt.EnhancedHashPassword(toHash, 12);

    public bool Verify(string toVerify, string hash) => BCrypt.Net.BCrypt.EnhancedVerify(toVerify, hash);

}