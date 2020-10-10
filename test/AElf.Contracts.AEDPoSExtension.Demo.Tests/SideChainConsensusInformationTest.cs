using System.Threading.Tasks;
using AElf.Standards.ACS10;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class SideChainConsensusInformationTest : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task SideChainDividendPoolTest()
        {
            InitialContracts();

            await ConsensusStub.SetSymbolList.SendWithExceptionAsync(new SymbolList());
            await ConsensusStub.GetDividends.CallAsync(new Int64Value {Value = 1});
        }
    }
}