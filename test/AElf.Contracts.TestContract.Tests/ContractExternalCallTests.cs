using System;
using System.Threading.Tasks;
using AElf.Contracts.TestContract;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contract.TestContract
{
    public class ContractExternalCallTests : TestContractTestBase
    {
        public ContractExternalCallTests()
        {
            InitializeTestContracts();
        }

        [Fact]
        public async Task Internal_Execute_And_Call()
        {
            //execute method
            var transactionResult = (await TestBasicFunctionContractStub.UserPlayBet.SendAsync(
                new BetInput
                {
                    Int64Value = 100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //call method
            var winData = (await TestBasicFunctionContractStub.QueryUserWinMoney.CallAsync(
                DefaultSender)).Int64Value;
            var loseData = (await TestBasicFunctionContractStub.QueryUserLoseMoney.CallAsync(
                DefaultSender)).Int64Value;
            (winData + loseData).ShouldBeGreaterThanOrEqualTo(100);
        }
        
        [Fact]
        public async Task External_Execute_And_Call()
        {
            //execute method
            var transactionResult = (await TestBasicSecurityContractStub.TestExecuteExternalMethod.SendAsync(
                new Int64Input
                {
                  Int64Value  = 100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //call method
            var winData = (await TestBasicFunctionContractStub.QueryUserWinMoney.CallAsync(
                DefaultSender)).Int64Value;
            if (winData > 0)
            {
                winData.ShouldBeGreaterThanOrEqualTo(100);
                return;
            }
            var loseData = (await TestBasicFunctionContractStub.QueryUserLoseMoney.CallAsync(
                DefaultSender)).Int64Value;
            (winData + loseData).ShouldBeLessThan(100);
            
        }

        [Fact]
        public async Task Internal_ExecuteFailed()
        {
            var transactionResult = (await TestBasicSecurityContractStub.TestExecuteExternalMethod.SendAsync(
                new Int64Input
                {
                    Int64Value  = 5
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

            var result = (await TestBasicSecurityContractStub.QueryInt64State.CallAsync(
                new Empty())).Int64Value;
            result.ShouldBe(0);
        }

        [Fact]
        public async Task External_ExecuteFailed()
        {
            await TestBasicSecurityContractStub.TestInt64State.SendAsync(
                new Int64Input
                {
                    Int64Value = Int64.MaxValue
                });
            
            var transactionResult = (await TestBasicSecurityContractStub.TestExecuteExternalMethod.SendAsync(
                new Int64Input
                {
                    Int64Value  = 500
                })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var winData = (await TestBasicFunctionContractStub.QueryUserWinMoney.CallAsync(
                DefaultSender)).Int64Value;
            winData.ShouldBe(0);
            
            var loseData = (await TestBasicFunctionContractStub.QueryUserLoseMoney.CallAsync(
                DefaultSender)).Int64Value;
            loseData.ShouldBe(0);
        }
    }
}