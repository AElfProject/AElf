using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        private static readonly ECKeyPair _ecKeyPair = CryptoHelpers.GenerateKeyPair();
        
        public static byte[] Sign(byte[] data)
        {
            return CryptoHelpers.SignWithPrivateKey(_ecKeyPair.PrivateKey, data);
        }

        public static byte[] GetPubicKey()
        {
            return _ecKeyPair.PublicKey;
        }
    }
}