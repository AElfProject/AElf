namespace AElf.Cryptography.Certificate
{
    public interface ICertificateStore
    {
        RSAKeyPair WriteKeyAndCertificate(string name, string ipAddress);
        string LoadCertificate(string name);
        string LoadKeyStore(string name);
    }
}