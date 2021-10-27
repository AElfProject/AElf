using AElf.Cryptography;

namespace AElf.Kernel.Account.Infrastructure
{
    public interface IAElfAsymmetricCipherKeyPairProvider
    {
        void SetKeyPair(IAElfAsymmetricCipherKeyPair ecKeyPair);
        IAElfAsymmetricCipherKeyPair GetKeyPair();
    }
}