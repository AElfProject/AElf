using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.Contract.CrossChain.Tests
{
    public class CrossChainContractTestHelper
    {
        public static ECKeyPair EcKeyPair { get; } =  CryptoHelpers.GenerateKeyPair();
        
        public static byte[] Sign(byte[] data)
        {
            return CryptoHelpers.SignWithPrivateKey(EcKeyPair.PrivateKey, data);
        }

        public static byte[] GetPubicKey()
        {
            return EcKeyPair.PublicKey;
        }

        public static Address GetAddress()
        {
            return Address.FromPublicKey(GetPubicKey());
        }
    }
}