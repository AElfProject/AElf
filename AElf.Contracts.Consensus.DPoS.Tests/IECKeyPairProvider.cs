using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.Contracts.Consensus.DPoS
{
    public interface IECKeyPairProvider
    {
        void SetECKeyPair(ECKeyPair ecKeyPair);
        ECKeyPair GetECKeyPair();
    }

    public class ECKeyPairProvider : IECKeyPairProvider
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