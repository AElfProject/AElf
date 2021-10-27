using AElf.Cryptography;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Account.Infrastructure
{
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
}