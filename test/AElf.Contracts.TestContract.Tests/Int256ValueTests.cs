using System.Threading.Tasks;
using AElf.Contracts.TestContract.BigIntValue;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contract.TestContract
{
    public class Int256ValueTests : TestContractTestBase
    {
        public Int256ValueTests()
        {
            InitializeTestContracts();
        }

        [Fact]
        public async Task BasicOperationTest()
        {
            {
                var transactionResult = (await BigIntValueContractStub.Add.SendAsync(new BigIntValueInput
                {
                    Foo =
                        "100_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000",
                    Bar = "100"
                })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var result = (await BigIntValueContractStub.Get.CallAsync(
                    new Empty())).Value;
                result.ShouldBe(
                    "100_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0100"
                        .Replace("_", string.Empty));
            }

            {
                var transactionResult = (await BigIntValueContractStub.Sub.SendAsync(new BigIntValueInput
                {
                    Foo =
                        "100_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0100",
                    Bar = "100"
                })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var result = (await BigIntValueContractStub.Get.CallAsync(
                    new Empty())).Value;
                result.ShouldBe(
                    "100_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000"
                        .Replace("_", string.Empty));
            }
        }
    }
}