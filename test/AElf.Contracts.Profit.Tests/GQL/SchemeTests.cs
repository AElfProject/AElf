using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit;

public partial class ProfitContractTests
{
    [Fact(DisplayName = "Try to create schemes with invalid input.")]
    public async Task ProfitContract_CreateScheme_With_Invalid_Input_Test()
    {
        var creator = Creators[0];

        var createSchemeRet = await creator.CreateScheme.SendWithExceptionAsync(new CreateSchemeInput
        {
            ProfitReceivingDuePeriodCount = ProfitContractTestConstants.MaximumProfitReceivingDuePeriodCount + 1,
        });
        createSchemeRet.TransactionResult.Error.ShouldContain("Invalid profit receiving due period count");

        createSchemeRet = await creator.CreateScheme.SendWithExceptionAsync(new CreateSchemeInput
        {
            ProfitReceivingDuePeriodCount = -1,
        });
        createSchemeRet.TransactionResult.Error.ShouldContain("Invalid profit receiving due period count");
    }
}