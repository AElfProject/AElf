using AElf.Cryptography.ECDSA;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Account.Application
{
    public interface IECKeyPairProvider
    {
        void SetECKeyPair(ECKeyPair ecKeyPair);
        ECKeyPair GetECKeyPair();
    }
    
    
    public class ECKeyPairProvider : IECKeyPairProvider, ISingletonDependency
    {
        private ECKeyPair _keyPair;

        public void SetECKeyPair(ECKeyPair ecKeyPair)
        {
            _keyPair = ecKeyPair;
        }

        public ECKeyPair GetECKeyPair()
        {
            return _keyPair;
        }
    }
}