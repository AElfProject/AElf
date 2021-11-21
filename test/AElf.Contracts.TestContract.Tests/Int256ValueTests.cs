using System.Threading.Tasks;
using AElf.Contracts.TestContract.Int256Value;
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
                var transactionResult = (await Int256ValueContractStub.Int256ValueAdd.SendAsync(new Int256ValueInput
                {
                    Foo =
                        "100_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000",
                    Bar = "100"
                })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var result = (await Int256ValueContractStub.GetInt256StateValue.CallAsync(
                    new Empty())).Value;
                result.ShouldBe(
                    "100_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0100"
                        .Replace("_", string.Empty));
            }
        }
    }
}