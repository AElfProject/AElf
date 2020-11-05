using System.Threading.Tasks;
using AElf.Standards.ACS10;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest
    {
        [Fact]
        public async Task SideChainDividendPool_Release_Test()
        {
            var result = await AEDPoSContractStub.Release.SendAsync(new ReleaseInput
            {
                PeriodNumber = 1
            });
            result.TransactionResult.Error.ShouldContain("Side chain dividend pool can only release automatically.");
        }

        [Fact]
        public async Task SideChainDividendPool_SetSymbolList_Test()
        {
            var result = await AEDPoSContractStub.SetSymbolList.SendAsync(new SymbolList());
            result.TransactionResult.Error.ShouldContain("Side chain dividend pool not support setting symbol list.");
        }

        [Fact]
        public async Task SideChainDividendPool_Views_Test()
        {
            var dividends = await AEDPoSContractStub.GetDividends.CallAsync(new Int64Value {Value = 1});
            dividends.Value.Count.ShouldBe(0);

            var symbolList = await AEDPoSContractStub.GetSymbolList.CallAsync(new Empty());
            symbolList.Value.Count.ShouldBe(0);

            var undistributedDividends = await AEDPoSContractStub.GetUndistributedDividends.CallAsync(new Empty());
            undistributedDividends.Value.Count.ShouldBe(0);
        }
    }
}