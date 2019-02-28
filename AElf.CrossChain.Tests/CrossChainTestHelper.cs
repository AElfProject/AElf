using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
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
        public static ISmartContractExecutiveService FakeSmartContractExecutiveService()
        {
            Mock<ISmartContractExecutiveService> mockObject = new Mock<ISmartContractExecutiveService>();
            mockObject.Setup(m => m.GetExecutiveAsync(It.IsAny<int>(), It.IsAny<IChainContext>(), It.IsAny<Address>()))
                .Returns<int, IChainContext, Address>((chainId, chainContext, address) => Task.FromResult(FakeExecutive()));
            return mockObject.Object;
        }

        private static IExecutive FakeExecutive()
        {
            Mock<IExecutive> mockObject = new Mock<IExecutive>();
            mockObject.Setup(m => m.SetTransactionContext(It.IsAny<TransactionContext>())).Returns<TransactionContext>(
                tc =>
                {
                    tc.Trace.RetVal.Data = new UInt64Value {Value = 1}.ToByteString();
                    return mockObject.Object;
                });
            mockObject.Setup(m => m.Apply()).Returns(()=> Task.CompletedTask);
            return mockObject.Object;
        }

        public static AccountOptions FakeAccountOption()
        {
            var nodeAccount = Address.FromPublicKey(EcKeyPair.PublicKey).GetFormatted();
            var nodeAccountPassword = "123";
            return new AccountOptions
            {
                NodeAccount = nodeAccount,
                NodeAccountPassword = nodeAccountPassword
            };
        }

        public static IKeyStore FakeKeyStore()
        {
            Mock<IKeyStore> mockObject = new Mock<IKeyStore>();
            mockObject.Setup(m => m.OpenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(AElfKeyStore.Errors.None));
            mockObject.Setup(m => m.GetAccountKeyPair(It.IsAny<string>())).Returns(EcKeyPair);
            return mockObject.Object;
        }

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