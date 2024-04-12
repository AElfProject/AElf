using System.Threading.Tasks;
using AElf.OS.Network.Application;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application;

public class BlockSyncAttachServiceAbnormalPeerTests : BlockSyncAttachBlockAbnormalPeerTestBase
{
    private readonly IBlockSyncAttachService _blockSyncAttachService;
    private readonly INetworkService _networkService;
    private readonly OSTestHelper _osTestHelper;

    public BlockSyncAttachServiceAbnormalPeerTests()
    {
        _blockSyncAttachService = GetRequiredService<IBlockSyncAttachService>();
        _osTestHelper = GetRequiredService<OSTestHelper>();
        _networkService = GetRequiredService<INetworkService>();
    }

    [Fact]
    public async Task Attach_InvalidBlock()
    {
        var abnormalPeerPubkey = "AbnormalPeerPubkey";
        var badBlock = _osTestHelper.GenerateBlockWithTransactions(HashHelper.ComputeFrom("BadBlock"), 10000);

        var abnormalPeer = _networkService.GetPeerByPubkey(abnormalPeerPubkey);
        abnormalPeer.ShouldNotBeNull();

        await _blockSyncAttachService.AttachBlockWithTransactionsAsync(badBlock, abnormalPeerPubkey);

        abnormalPeer = _networkService.GetPeerByPubkey(abnormalPeerPubkey);
        abnormalPeer.ShouldBeNull();
    }
}