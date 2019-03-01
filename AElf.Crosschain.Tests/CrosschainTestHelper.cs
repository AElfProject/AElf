using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        public static ECKeyPair EcKeyPair = CryptoHelpers.GenerateKeyPair();
        
        public static byte[] Sign(byte[] data)
        {
            return CryptoHelpers.SignWithPrivateKey(EcKeyPair.PrivateKey, data);
        }

        public static byte[] GetPubicKey()
        {
            return EcKeyPair.PublicKey;
        }
    }
}