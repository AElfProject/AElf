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
        public async Task ProfitContract_SetMethodFee_WithoutPermission_Test()
        {
            //no permission
            var transactionResult = await ProfitContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
            {
                MethodName = "OnlyTest"
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("Unauthorized to set method fee.");
        }
    }
}