using System;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contract.TestContract
{
    public class ContractSecurityTests : TestContractTestBase
    {
        public ContractSecurityTests()
        {
            InitializeTestContracts();
        }

        [Fact]
        public async Task Int32_OverFlow_UpperLimit()
        {
            var transactionResult = (await TestBasicSecurityContractStub.TestInt32State.SendAsync(
                new Int32Input
                {
                    Int32Value = Int32.MaxValue
                })).TransactionResult;

            var resultValue = (await TestBasicSecurityContractStub.QueryInt32State.CallAsync(
                new Empty())).Int32Value;
            resultValue.ShouldBe(Int32.MaxValue);

            transactionResult = (await TestBasicSecurityContractStub.TestInt32State.SendAsync(
                new Int32Input
                {
                    Int32Value = 100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("System.OverflowException").ShouldBeTrue();

            //state not change
            var resultValue1 = (await TestBasicSecurityContractStub.QueryInt32State.CallAsync(
                new Empty())).Int32Value;
            resultValue1.ShouldBe(Int32.MaxValue);
        }
        
        [Fact]
        public async Task Int32_OverFlow_LowerLimit()
        {
            var transactionResult = (await TestBasicSecurityContractStub.TestInt32State.SendAsync(
                new Int32Input
                {
                    Int32Value = Int32.MinValue
                })).TransactionResult;

            var resultValue = (await TestBasicSecurityContractStub.QueryInt32State.CallAsync(
                new Empty())).Int32Value;
            resultValue.ShouldBe(Int32.MinValue);

            transactionResult = (await TestBasicSecurityContractStub.TestInt32State.SendAsync(
                new Int32Input
                {
                    Int32Value = -100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("System.OverflowException").ShouldBeTrue();

            //state not change
            var resultValue1 = (await TestBasicSecurityContractStub.QueryInt32State.CallAsync(
                new Empty())).Int32Value;
            resultValue1.ShouldBe(Int32.MinValue);
        }

        [Fact]
        public async Task Int64_OverFlow_UpperLimit()
        {
            var transactionResult = (await TestBasicSecurityContractStub.TestInt64State.SendAsync(
                new Int64Input()
                {
                    Int64Value = Int64.MaxValue
                })).TransactionResult;

            var resultValue = (await TestBasicSecurityContractStub.QueryInt64State.CallAsync(
                new Empty())).Int64Value;
            resultValue.ShouldBe(Int64.MaxValue);

            transactionResult = (await TestBasicSecurityContractStub.TestInt64State.SendAsync(
                new Int64Input
                {
                    Int64Value = 100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("System.OverflowException").ShouldBeTrue();

            //state not change
            var resultValue1 = (await TestBasicSecurityContractStub.QueryInt64State.CallAsync(
                new Empty())).Int64Value;
            resultValue1.ShouldBe(Int64.MaxValue);
        }
        
        [Fact]
        public async Task Int64_OverFlow_LowerLimit()
        {
            var transactionResult = (await TestBasicSecurityContractStub.TestInt64State.SendAsync(
                new Int64Input()
                {
                    Int64Value = Int64.MinValue
                })).TransactionResult;

            var resultValue = (await TestBasicSecurityContractStub.QueryInt64State.CallAsync(
                new Empty())).Int64Value;
            resultValue.ShouldBe(Int64.MinValue);

            transactionResult = (await TestBasicSecurityContractStub.TestInt64State.SendAsync(
                new Int64Input
                {
                    Int64Value = -100
                })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("System.OverflowException").ShouldBeTrue();

            //state not change
            var resultValue1 = (await TestBasicSecurityContractStub.QueryInt64State.CallAsync(
                new Empty())).Int64Value;
            resultValue1.ShouldBe(Int64.MinValue);
        }
    }
}