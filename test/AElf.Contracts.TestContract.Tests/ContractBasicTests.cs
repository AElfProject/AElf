using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestContract;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
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
                new AElf.Contracts.TestContract.BasicFunction.InitialBasicContractInput
                {
                    ContractName = "Test initialize again",
                    MinValue = 1000,
                    MaxValue = 10000,
                    Manager = SampleAddress.AddressList[0]
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateContract_WithOwner_Success()
        {
            var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                new Acs0.ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("BasicUpdate")).Value)
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
                new Acs0.ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("BasicFunction")).Value)
                }
            )).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Code is not changed").ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateContract_And_Call_Old_Method()
        {
            var transactionResult = (await BasicContractZeroStub.UpdateSmartContract.SendAsync(
                new Acs0.ContractUpdateInput
                {
                    Address = BasicFunctionContractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("BasicUpdate")).Value)
                }
            )).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //execute new action method
            transactionResult = (await TestBasicFunctionContractStub.UserPlayBet.SendAsync(
                new AElf.Contracts.TestContract.BasicFunction.BetInput
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
        public async Task ChangeAuthor_Without_Permission_Failed()
        {
            var otherUser = SampleECKeyPairs.KeyPairs[2];
            var otherZeroStub = GetContractZeroTester(otherUser);
            var transactionResult = (await otherZeroStub.ChangeContractAuthor.SendAsync(
                new Acs0.ChangeContractAuthorInput()
                {
                    ContractAddress = BasicFunctionContractAddress,
                    NewAuthor = SampleAddress.AddressList[1]
                }
            )).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("no permission").ShouldBeTrue();
        }

        [Fact]
        public async Task ChangeAuthor_With_Permission_Success()
        {
            var otherUser = SampleAddress.AddressList[2];
            var transactionResult = (await BasicContractZeroStub.ChangeContractAuthor.SendAsync(
                new Acs0.ChangeContractAuthorInput()
                {
                    ContractAddress = BasicFunctionContractAddress,
                    NewAuthor = otherUser
                }
            )).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var ownerAddress = (await BasicContractZeroStub.GetContractAuthor.CallAsync(BasicFunctionContractAddress))
                .GetFormatted();
            ownerAddress.ShouldBe(otherUser.GetFormatted());
        }
    }
}