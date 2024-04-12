using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync;
using AElf.OS.BlockSync.Application;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers;

public class BlockReceivedEventHandler : ILocalEventHandler<BlockReceivedEvent>, ITransientDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly BlockSyncOptions _blockSyncOptions;
    private readonly IBlockSyncService _blockSyncService;
    private readonly IBlockSyncValidationService _blockSyncValidationService;

    public BlockReceivedEventHandler(IBlockSyncService blockSyncService,
        IBlockSyncValidationService blockSyncValidationService,
        IBlockchainService blockchainService,
        IOptionsSnapshot<BlockSyncOptions> blockSyncOptions)
    {
        _blockSyncService = blockSyncService;
        _blockSyncValidationService = blockSyncValidationService;
        _blockchainService = blockchainService;
        _blockSyncOptions = blockSyncOptions.Value;

        Logger = NullLogger<BlockReceivedEventHandler>.Instance;
    }

    public ILogger<BlockReceivedEventHandler> Logger { get; set; }

    public Task HandleEventAsync(BlockReceivedEvent eventData)
    {
        var _ = ProcessNewBlockAsync(eventData.BlockWithTransactions, eventData.SenderPubkey);
        return Task.CompletedTask;
    }

    private async Task ProcessNewBlockAsync(BlockWithTransactions blockWithTransactions, string senderPubkey)
    {
        var chain = await _blockchainService.GetChainAsync();

        Logger.LogDebug(
            $"About to process new block: {blockWithTransactions.Header.GetHash().ToHex()} of height {blockWithTransactions.Height}");

        if (!await _blockSyncValidationService.ValidateBlockBeforeSyncAsync(chain, blockWithTransactions,
                senderPubkey))
            return;

        await _blockSyncService.SyncByBlockAsync(chain, new SyncBlockDto
        {
            BlockWithTransactions = blockWithTransactions,
            BatchRequestBlockCount = _blockSyncOptions.MaxBatchRequestBlockCount,
            SuggestedPeerPubkey = senderPubkey
        });
    }
}