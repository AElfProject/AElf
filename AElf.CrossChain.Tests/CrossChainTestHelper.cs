using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        public static readonly ECKeyPair EcKeyPair = CryptoHelpers.GenerateKeyPair();
        
        public static Dictionary<int, long> SideChainIdHeights;
        public static Dictionary<int, long> ParentChainIdHeight;
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