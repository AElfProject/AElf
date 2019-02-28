using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.OS;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Moq;

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