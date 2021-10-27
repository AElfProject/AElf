using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using CustomContract = AElf.Runtime.CSharp.Tests.TestContract;

namespace AElf.Sdk.CSharp.Tests
{
    public class CSharpSmartContractContextTests : SdkCSharpTestBase
    {
        [Fact]
        public void Verify_Transaction_Origin_SetValue()
        {
            var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
            var origin = SampleAddress.AddressList[0];
            bridgeContext.TransactionContext = new TransactionContext
            {
                Origin = origin,
                Transaction = new Transaction()
                {
                    From = SampleAddress.AddressList[1],
                    To = SampleAddress.AddressList[2]
                }
            };
            var contractContext = new CSharpSmartContractContext(bridgeContext);
            contractContext.Origin.ShouldBe(origin); 
            contractContext.Origin.ShouldNotBe(bridgeContext.TransactionContext.Transaction.From);
        }

        [Fact]
        public void Transaction_VerifySignature_Test()
        {
            var keyPair = Cryptography.CryptoHelper.GenerateKeyPair();
            var transaction = new Transaction
            {
                From = Address.FromPublicKey(keyPair.PublicKey),
                To = SampleAddress.AddressList[2],
                MethodName = "Test",
                RefBlockNumber = 100
            };
            var signature = Cryptography.CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            
            var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
            var result = bridgeContext.VerifySignature(transaction);
            result.ShouldBeTrue();
        }
    }
}