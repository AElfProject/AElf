using System.IO;
using System.Threading.Tasks;
using AElf.Contracts.TestContract;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicUpdate;
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
            var transactionResult = (await TestBasicFunctionContractStub.InitialBasicFunctionContract.SendAsync(
                new InitialBasicContractInput
                {
                    ContractName = "Test initialize again",
                    MinValue = 1000,
                    MaxValue = 10000,
                    Manager = Address.Generate()
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        [Fact(Skip = "Failed to find handler for UpdateStopBet.")]
        public async Task UpdateContract_WithOwner_Success()
        {
            var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                new ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(BasicUpdateContract).Assembly.Location)) 
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var basic11ContractStub = GetTestBasicUpdateContractStub(DefaultSenderKeyPair);
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
        public async Task UpdateContract_WithSameCode_Failed()
        {
            var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                new ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(BasicFunctionContract).Assembly.Location)) 
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Code is not changed").ShouldBeTrue();
        } 
        
        [Fact]
        public async Task UpdateContract_And_Call_Old_Method()
        {
            var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                new ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(BasicUpdateContract).Assembly.Location)) 
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //execute new action method
            transactionResult = (await TestBasicFunctionContractStub.UserPlayBet.SendAsync(
                new BetInput
                {
                    Int64Value = 100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //check result
            var winData = (await TestBasicFunctionContractStub.QueryUserWinMoney.CallAsync(
                DefaultSender)).Int64Value;
            if (winData > 0)
            {
                winData.ShouldBeGreaterThanOrEqualTo(100);
                return;
            }
            var loseData = (await TestBasicFunctionContractStub.QueryUserLoseMoney.CallAsync(
                DefaultSender)).Int64Value;
            (winData + loseData).ShouldBe(100);
        }

        [Fact]
        public async Task UpdateContract_Without_Permission_Failed()
        {
            var otherUser = SampleECKeyPairs.KeyPairs[2];
            var otherZeroStub = GetContractZeroTester(otherUser);
            var transactionResult = (await otherZeroStub.UpdateSmartContract.SendAsync(
                new ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(BasicUpdateContract).Assembly.Location)) 
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Only owner is allowed to update code").ShouldBeTrue();
        }

        [Fact]
        public async Task ChangeOwner_Without_Permission_Failed()
        {
            var otherUser = SampleECKeyPairs.KeyPairs[2];
            var otherZeroStub = GetContractZeroTester(otherUser);
            var transactionResult = (await otherZeroStub.ChangeContractOwner.SendAsync(
                new ChangeContractOwnerInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    NewOwner = Address.Generate()
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("no permission").ShouldBeTrue();
        }
        
        [Fact]
        public async Task ChangeOwner_With_Permission_Success()
        {
            var otherUser = Address.Generate();
            var transactionResult = (await BasicContractZeroStub.ChangeContractOwner.SendAsync(
                new ChangeContractOwnerInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    NewOwner = otherUser
                }
            )).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var ownerAddress = (await BasicContractZeroStub.GetContractOwner.CallAsync(BasicFunctionContractAddress)).GetFormatted();
            ownerAddress.ShouldBe(otherUser.GetFormatted());
        }
    }
}