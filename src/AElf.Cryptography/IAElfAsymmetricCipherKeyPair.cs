namespace AElf.Cryptography
{
    public interface IAElfAsymmetricCipherKeyPair
    {
        byte[] PrivateKey { get; }
        byte[] PublicKey { get; }
    }
}