using System.Threading.Tasks;
using Acs10;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class SideChainConsensusTest : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task SideChainDividendPoolTest()
        {
            await ConsensusStub.SetSymbolList.SendWithExceptionAsync(new SymbolList());

            await ConsensusStub.GetDividends.CallAsync(new Int64Value {Value = 1});
        }
    }
}