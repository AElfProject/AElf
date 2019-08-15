using System.Threading.Tasks;
using Acs1;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit.BVT
{
    public partial class ProfitContractTests
    {
        [Fact]
        public async Task ProfitContract_SetMethodFee_WithoutPermission()
        {
            //no permission
            var transactionResult = await ProfitContractStub.SetMethodFee.SendAsync(new TokenAmounts
            {
                Method = "OnlyTest"
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("Assertion failed");
        }
    }
}