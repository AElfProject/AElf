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
                while (blocks.Any())
                {
                    _logger?.Trace($"Will get block of height {inHeight.TargetHeight + i} from block set to execute.");
                    i++;
                    foreach (var block in blocks)
                    {
                        if (await ReceiveBlock(block) == BlockValidationResult.Success)
                        {
                            if (await BlockChain.HasBlock(block.GetHash()))
                            {
                                return;
                            }
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
            var executionResult = await _blockExecutor.ExecuteBlock(message.Block);
            if (executionResult == BlockExecutionResult.Success)
            {
                _blockSet.Tell(message.Block.Index);
                MessageHub.Instance.Publish(message);
                MessageHub.Instance.Publish(UpdateConsensus.Update);
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(message.Block.Index + 1));
            }
            else
            {
                
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
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(currentHeight + 1));
            
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
        
        public bool IsBlockReceived(Hash blockHash, ulong height)
        {
            return _blockSet.IsBlockReceived(blockHash, height) || BlockChain.HasBlock(blockHash).Result;
        }

        public IBlock GetBlockByHash(Hash blockHash)
        {
            return _blockSet.GetBlockByHash(blockHash) ?? BlockChain.GetBlockByHashAsync(blockHash).Result;
        }

        public List<IBlock> GetBlockByHeight(ulong height)
        {
            return _blockSet.GetBlockByHeight(height) ?? new List<IBlock>
            {
                BlockChain.GetBlockByHeightAsync(height).Result
            };
        }
    }
}