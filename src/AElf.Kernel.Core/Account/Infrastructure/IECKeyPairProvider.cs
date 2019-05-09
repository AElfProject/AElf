using AElf.Cryptography.ECDSA;
using Volo.Abp.DependencyInjection;
// ReSharper disable InconsistentNaming

namespace AElf.Kernel.Account.Infrastructure
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