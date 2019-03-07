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
        
        public readonly Dictionary<int, long> SideChainIdHeights = new Dictionary<int, long>();
        public readonly Dictionary<int, long> ParentChainIdHeight = new Dictionary<int, long>();
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

        public void AddSideChainIdHeight(int sideChainId, long height)
        {
            SideChainIdHeights.Add(sideChainId, height);
        }

        public void AddParentChainIdHeight(int parentChainId, long height)
        {
            ParentChainIdHeight.Add(parentChainId, height);
        }
    }
}