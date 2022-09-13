using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.BlockSync.Events;
using AElf.OS.Network;
using AElf.OS.Network.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.BlockSync.Application;

public class BlockSyncAttachService : IBlockSyncAttachService
{
    private readonly IBlockAttachService _blockAttachService;
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockSyncQueueService _blockSyncQueueService;
    private readonly IBlockSyncValidationService _blockSyncValidationService;

    public BlockSyncAttachService(IBlockchainService blockchainService,
        IBlockAttachService blockAttachService,
        IBlockSyncValidationService blockSyncValidationService,
        IBlockSyncQueueService blockSyncQueueService)
    {
        Logger = NullLogger<BlockSyncAttachService>.Instance;
        LocalEventBus = NullLocalEventBus.Instance;

        _blockchainService = blockchainService;
        _blockAttachService = blockAttachService;
        _blockSyncValidationService = blockSyncValidationService;
        _blockSyncQueueService = blockSyncQueueService;
    }

    public ILocalEventBus LocalEventBus { get; set; }

    public ILogger<BlockSyncAttachService> Logger { get; set; }

    public async Task AttachBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions,
        string senderPubkey, Func<Task> attachFinishedCallback = null)
    {
        var blockValid = await _blockSyncValidationService.ValidateBlockBeforeAttachAsync(blockWithTransactions);
        if (!blockValid)
        {
            Logger.LogDebug(
                $"Sync block validation failed, peer: {senderPubkey}, block hash: {blockWithTransactions.GetHash()}, block height: {blockWithTransactions.Height}");
            await LocalEventBus.PublishAsync(new AbnormalPeerFoundEventData
            {
                BlockHash = blockWithTransactions.GetHash(),
                BlockHeight = blockWithTransactions.Height,
                PeerPubkey = senderPubkey
            });

            return;
        }

        await _blockchainService.AddTransactionsAsync(blockWithTransactions.Transactions);
        var block = blockWithTransactions.ToBlock();
        await _blockchainService.AddBlockAsync(block);

        _blockSyncQueueService.Enqueue(async () =>
            {
                try
                {
                    await _blockAttachService.AttachBlockAsync(block);
                }
                finally
                {
                    if (attachFinishedCallback != null) await attachFinishedCallback();
                }
            },
            KernelConstants.UpdateChainQueueName);
    }
}