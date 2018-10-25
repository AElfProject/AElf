using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.Synchronization.BlockSynchronization
{
    public class BlockSynchronizer : IBlockSynchronizer
    {
        private readonly IChainService _chainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockExecutor _blockExecutor;

        private readonly IBlockSet _blockSet;

        private IBlockChain _blockChain;

        private readonly ILogger _logger;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(NodeConfig.Instance.ChainId)));

        private bool _receivedBranchedBlock;

        private const ulong Limit = 256;

        public BlockSynchronizer(IChainService chainService, IBlockValidationService blockValidationService,
            IBlockExecutor blockExecutor, IBlockSet blockSet)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            _blockExecutor = blockExecutor;
            _blockSet = blockSet;

            _logger = LogManager.GetLogger(nameof(BlockSynchronizer));

            MessageHub.Instance.Subscribe<SyncUnfinishedBlock>(async inHeight =>
            {
                // Find new blocks from block set to execute
                var blocks = _blockSet.GetBlockByHeight(inHeight.TargetHeight);
                ulong i = 0;
                while (blocks != null && blocks.Any())
                {
                    _logger?.Trace(
                        $"Will get block of height {inHeight.TargetHeight + i} from block set to execute - {blocks.Count} blocks.");
                    i++;
                    foreach (var block in blocks)
                    {
                        blocks = _blockSet.GetBlockByHeight(inHeight.TargetHeight + i);
                        await ReceiveBlock(block);
                    }
                }
            });
        }

        public async Task<BlockExecutionResult> ReceiveBlock(IBlock block)
        {
            var blockValidationResult =
                await _blockValidationService.ValidatingOwnBlock(false).ValidateBlockAsync(block, await GetChainContextAsync());

            var message = new BlockExecuted(block, blockValidationResult);

            if (blockValidationResult.IsSuccess())
            {
                _logger?.Trace($"Valid Block {block.BlockHashToHex}.");

                return await HandleValidBlock(message);
            }

            _logger?.Warn($"Invalid Block {block.BlockHashToHex} : {message.BlockValidationResult.ToString()}.");
            await HandleInvalidBlock(message);

            return BlockExecutionResult.NotExecuted;
        }

        public async Task ReceiveBlocks(IEnumerable<IBlock> blocks)
        {
            if (blocks == null)
            {
                return;
            }

            foreach (var block in blocks.OrderBy(b => b.Index))
            {
                await ReceiveBlock(block);
            }
        }

        public void AddMinedBlock(IBlock block)
        {
            _blockSet.Tell(block);

            // Update DPoS process.
            MessageHub.Instance.Publish(UpdateConsensus.Update);

            // Basically notify the network layer that this node just mined a block
            // and added to executed block list.
            MessageHub.Instance.Publish(new BlockAddedToSet(block));
            
            // We can say the "initial sync" is finished, set KeepHeight to a specific number
            if (_blockSet.KeepHeight == ulong.MaxValue)
            {
                _logger?.Trace("Set the limit of the branched blocks cache in block set to " + Limit);
                _blockSet.KeepHeight = Limit;
            }
        }

        private async Task<BlockExecutionResult> HandleValidBlock(BlockExecuted message)
        {
            _blockSet.AddBlock(message.Block);

            var executionResult = await _blockExecutor.ExecuteBlock(message.Block);

            if (executionResult.NeedToRollback())
            {
                // Need to rollback one block:
                await BlockChain.RollbackOneBlock();
                _blockSet.InformRollback(message.Block.Index, message.Block.Index);

                // Basically re-sync the block of specific height.
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(message.Block.Index));

                return executionResult;
            }

            if (executionResult.CannotExecute())
            {
                _logger?.Trace($"Cannot execute block {message.Block.BlockHashToHex} of height {message.Block.Index}");
                return executionResult;
            }

            if (executionResult.CanExecuteAgain())
            {
                // No need to rollback:
                // Receive again to execute the same block.
                await ReceiveBlock(message.Block);

                return executionResult;
            }

            _blockSet.Tell(message.Block);

            // Notify the network layer the block has been executed.
            MessageHub.Instance.Publish(message);

            // Update the consensus information.
            MessageHub.Instance.Publish(UpdateConsensus.Update);
            
            MessageHub.Instance.Publish(new SyncUnfinishedBlock(message.Block.Index + 1));

            return BlockExecutionResult.Success;
        }

        private async Task HandleInvalidBlock(BlockExecuted message)
        {
            // Handle the invalid blocks according to their validation results.
            if ((int) message.BlockValidationResult < 100)
            {
                _blockSet.AddBlock(message.Block);
            }

            if (message.BlockValidationResult == BlockValidationResult.Unlinkable)
            {
                _logger?.Warn("Received unlinkable block.");
                
                _receivedBranchedBlock = true;

                await ReviewBlockSet();
            }

            // Received blocks from branched chain.
            if (message.BlockValidationResult == BlockValidationResult.BranchedBlock)
            {
                _logger?.Warn("Received a block from branched chain.");

                var linkableBlock = CheckLinkabilityOfBlock(message.Block);
                if (linkableBlock == null)
                {
                    return;
                }

                //await ReceiveBlock(linkableBlock);
            }

            if (message.BlockValidationResult == BlockValidationResult.Pending)
            {
                await ReviewBlockSet();
            }
        }

        /// <summary>
        /// Return true if there exists a block in block set is linkable to provided block.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private IBlock CheckLinkabilityOfBlock(IBlock block)
        {
            try
            {
                var checkIndex = block.Index - 1;
                var checkBlocks = _blockSet.GetBlockByHeight(checkIndex);
                if (checkBlocks == null || !checkBlocks.Any())
                {
                    // TODO: Launch a event to request missing blocks.

                    return null;
                }

                foreach (var checkBlock in checkBlocks)
                {
                    if (checkBlock.BlockHashToHex == block.Header.PreviousBlockHash.DumpHex())
                    {
                        return checkBlock;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                _logger?.Error($"Error while checking linkablity of block {block.BlockHashToHex} in height {block.Index}");
                return null;
            }
        }

        private async Task ReviewBlockSet()
        {
            if (!_receivedBranchedBlock)
            {
                return;
            }

            // In case of the block set exists blocks that should be valid but didn't executed yet.
            var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();

            // Detect longest chain and switch.
            var forkHeight = _blockSet.AnyLongerValidChain(currentHeight);
            if (forkHeight != 0)
            {
                RollbackToHeight(forkHeight);
                _blockSet.InformRollback(forkHeight, currentHeight);
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(forkHeight));
            }
        }

        private void RollbackToHeight(ulong targetHeight)
        {
            BlockChain.RollbackToHeight(targetHeight - 1).ConfigureAwait(false);
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
            
            if (chainContext.BlockHash != Hash.Genesis && chainContext.BlockHash != null)
            {
                chainContext.BlockHeight =
                    ((BlockHeader) await blockchain.GetHeaderByHashAsync(chainContext.BlockHash)).Index;
            }

            return chainContext;
        }

        public bool IsBlockReceived(Hash blockHash, ulong height)
        {
            return _blockSet.IsBlockReceived(blockHash, height) || BlockChain.HasBlock(blockHash).Result;
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            return _blockSet.GetBlockByHash(blockHash) ?? BlockChain.GetBlockByHashAsync(blockHash).Result;
        }

        public List<IBlock> GetBlocksByHeight(ulong height)
        {
            return _blockSet.GetBlockByHeight(height) ?? new List<IBlock>
            {
                BlockChain.GetBlockByHeightAsync(height).Result
            };
        }
    }
}