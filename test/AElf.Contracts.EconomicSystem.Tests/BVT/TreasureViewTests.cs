using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public async Task GetCurrentWelfareReward()
        {
            var reward = await TreasuryContractStub.GetCurrentTreasuryBalance.CallAsync(new Empty());
            reward.Value.ShouldBeGreaterThanOrEqualTo(0);
        }
    }
}