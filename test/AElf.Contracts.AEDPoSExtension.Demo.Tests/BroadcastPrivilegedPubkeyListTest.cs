using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class BroadcastPrivilegedPubkeyListTest : AEDPoSExtensionDemoTestBase
    {
        // Just test very basic logic of GetPubkeyList method because currently there's no consensus extra data in block header.
        [Fact]
        public async Task BroadcastPrivilegedPubkeyListProviderTest()
        {
            await BlockMiningService.MineBlockAsync();
            var broadcastPrivilegedPubkeyListProvider = GetRequiredService<IBroadcastPrivilegedPubkeyListProvider>();
            var blockchainService = GetRequiredService<IBlockchainService>();
            var chain = await blockchainService.GetChainAsync();
            var blockHeader = await blockchainService.GetBlockHeaderByHashAsync(chain.BestChainHash);
            var pubkeyList = await broadcastPrivilegedPubkeyListProvider.GetPubkeyList(blockHeader);
            pubkeyList.ShouldNotBeNull();
        }
    }
}