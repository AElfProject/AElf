using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public partial class EconomicTestBase
    {
        protected async Task<long> GetBalance(Address owner)
        {
            return (await TokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = owner,
                Symbol = EconomicTestConstants.TokenSymbol
            })).Balance;
        }
    }
}