using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using Easy.MessageHub;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IChainService _chainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockExecutionService _blockExecutionService;

        private readonly IBlockSet _blockSet;

        private IBlockChain _blockChain;

        private readonly ILogger _logger;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(NodeConfig.Instance.ChainId)));

        public BlockSyncService(IChainService chainService, IBlockValidationService blockValidationService,
            IBlockExecutionService blockExecutionService, IBlockSet blockSet)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            _blockExecutionService = blockExecutionService;
            _blockSet = blockSet;

            _logger = LogManager.GetLogger(nameof(BlockSyncService));

            MessageHub.Instance.Subscribe<SyncUnfinishedBlock>(async inHeight =>
            {
                // Find new blocks from block set to execute
                var blocks = _blockSet.GetBlockByHeight(inHeight.TargetHeight);
                ulong i = 0;
                while (blocks.Any())
                {
                    i++;
                    foreach (var block in blocks)
                    {
                        if (await ReceiveBlock(block) == BlockValidationResult.Success)
                        {
                            if (await BlockChain.HasBlock(block.GetHash()))
                            {
                                return;
                            }
                            _logger?.Trace($"Executed block of height {inHeight.TargetHeight + i - 1}.");
                            blocks = _blockSet.GetBlockByHeight(inHeight.TargetHeight + i);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            });
        }

        public async Task<BlockValidationResult> ReceiveBlock(IBlock block)
        {
            var blockValidationResult =
                await _blockValidationService.ValidateBlockAsync(block, await GetChainContextAsync());

            var message = new BlockAccepted(block, blockValidationResult);

            if (blockValidationResult == BlockValidationResult.Success)
            {
                _logger?.Trace($"Valid Block {block.GetHash().DumpHex()}.");
                await HandleValidBlock(message);
            }
            else
            {
                _logger?.Warn($"Invalid Block {block.GetHash().DumpHex()} : {message.BlockValidationResult.ToString()}.");
                await HandleInvalidBlock(message);
            }

            return blockValidationResult;
        }

        public void AddMinedBlock(IBlock block)
        {
            _blockSet.AddBlock(block);
            _blockSet.Tell(block.Header.Index);
            MessageHub.Instance.Publish(UpdateConsensus.Update);
            MessageHub.Instance.Publish(new BlockAddedToSet(block));
        }

        private async Task HandleValidBlock(BlockAccepted message)
        {
            _blockSet.AddBlock(message.Block);
            var executionResult = await _blockExecutionService.ExecuteBlock(message.Block);
            if (executionResult == BlockExecutionResultCC.Success)
            {
                _blockSet.Tell(message.Block.Header.Index);
                MessageHub.Instance.Publish(message);
                MessageHub.Instance.Publish(UpdateConsensus.Update);
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(message.Block.Header.Index + 1));
            }
            else
            {
                await _blockExecutionService.Rollback(message.Block.Body.TransactionList.ToList());
            }
        }

        private async Task HandleInvalidBlock(BlockAccepted message)
        {
            // Handle the invalid block according their validation result.
            if ((int) message.BlockValidationResult < 100)
            {
                _blockSet.AddBlock(message.Block);
            }

            await ReviewBlockSet(message);
        }
        
        private async Task ReviewBlockSet(BlockAccepted message)
        {
            // In case of the block set exists blocks that should be valid but didn't executed yet.
            var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();
            if (message.Block.Header.Index > currentHeight)
            {
                _logger?.Trace($"Need to execute blocks from {currentHeight + 1}");
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(currentHeight + 1));
            }
            
            // Detect longest chain and switch.
            var forkHeight = _blockSet.AnyLongerValidChain(currentHeight);
            if (forkHeight != 0)
            {
                _logger?.Trace("Will rollback to height: " + forkHeight);
                await BlockChain.RollbackToHeight(forkHeight);
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(forkHeight + 1));
            }
        }

        private async Task<IChainContext> GetChainContextAsync()
        {
            var chainId = Hash.LoadHex(NodeConfig.Instance.ChainId);
            var blockchain = _chainService.GetBlockChain(chainId);
            IChainContext chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = await blockchain.GetCurrentBlockHashAsync()
            };
            if (chainContext.BlockHash != Hash.Genesis)
            {
                chainContext.BlockHeight =
                    ((BlockHeader) await blockchain.GetHeaderByHashAsync(chainContext.BlockHash)).Index;
            }

            return chainContext;
        }
    }
}