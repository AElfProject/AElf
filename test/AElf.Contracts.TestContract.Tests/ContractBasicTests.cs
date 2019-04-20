using System.IO;
using System.Threading.Tasks;
using AElf.Contracts.TestContract;
using AElf.Contracts.TestContract.Basic11;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contract.TestContract
{
    public class ContractBasicTests : TestContractTestBase
    {
        public ContractBasicTests()
        {
            InitializeTestContracts();
        }

        [Fact]
        public async Task Initialize_MultiTimesContract()
        {
            var transactionResult = (await TestBasic1ContractStub.InitialBasic1Contract.SendAsync(
                new InitialBasic1ContractInput
                {
                    ContractName = "Test initialize again",
                    MinValue = 1000,
                    MaxValue = 10000
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        [Fact(Skip = "Failed to find handler for UpdateStopBet. We have 7 handlers.")]
        public async Task UpdateContract_WithOwner_Success()
        {
            var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                new ContractUpdateInput
                {
                    Address = Basic1ContractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(Basic11Contract).Assembly.Location)) 
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var basic11ContractStub = GetTestBasic11ContractStub(DefaultSenderKeyPair);
            //execute new action method
            var transactionResult1 = (await basic11ContractStub.UpdateStopBet.SendAsync(
                new Empty())).TransactionResult;
            transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //call new view method
            var result = (await basic11ContractStub.QueryBetStatus.CallAsync(
                new Empty())).BoolValue;
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateContract_WithoutOwner_Failed()
        {
            var otherUser = SampleECKeyPairs.KeyPairs[2];
            var otherZeroStub = GetContractZeroTester(otherUser);
            var transactionResult = (await otherZeroStub.UpdateSmartContract.SendAsync(
                new ContractUpdateInput
                {
                    Address = Basic1ContractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(Basic11Contract).Assembly.Location)) 
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Only owner is allowed to update code").ShouldBeTrue();
        }

        [Fact]
        public async Task ChangeOwner_WithoutOwner()
        {
            var otherUser = SampleECKeyPairs.KeyPairs[2];
            var otherZeroStub = GetContractZeroTester(otherUser);
            var transactionResult = (await otherZeroStub.ChangeContractOwner.SendAsync(
                new ChangeContractOwnerInput
                {
                    ContractAddress = Basic1ContractAddress,
                    NewOwner = Address.Generate()
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("no permission").ShouldBeTrue();
        }
        
        [Fact]
        public async Task ChangeOwner_WithOwner()
        {
            var otherUser = Address.Generate();
            var transactionResult = (await BasicContractZeroStub.ChangeContractOwner.SendAsync(
                new ChangeContractOwnerInput
                {
                    ContractAddress = Basic1ContractAddress,
                    NewOwner = otherUser
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var ownerAddress = (await BasicContractZeroStub.GetContractOwner.CallAsync(Basic1ContractAddress)).GetFormatted();
            ownerAddress.ShouldBe(otherUser.GetFormatted());
        }
    }
}