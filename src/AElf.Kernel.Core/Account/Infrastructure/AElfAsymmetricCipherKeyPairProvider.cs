using AElf.Cryptography;

namespace AElf.Kernel.Account.Infrastructure;

public class AElfAsymmetricCipherKeyPairProvider : IAElfAsymmetricCipherKeyPairProvider, ISingletonDependency
{
    private IAElfAsymmetricCipherKeyPair _keyPair;

    public void SetKeyPair(IAElfAsymmetricCipherKeyPair ecKeyPair)
    {
        _keyPair = ecKeyPair;
    }

    public IAElfAsymmetricCipherKeyPair GetKeyPair()
    {
        return _keyPair;
    }
}