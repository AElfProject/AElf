using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.Managers;
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
                _logger?.Trace($"Block {block.GetHash().DumpHex()} is a valid block.");
                await HandleValidBlock(message);
            }
            else
            {
                _logger?.Trace($"Block {block.GetHash().DumpHex()} is an invalid block.");
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
            // TODO: else
        }

        private async Task HandleInvalidBlock(BlockAccepted message)
        {
            _blockSet.AddBlock(message.Block);

            var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();
            if (message.Block.Header.Index > currentHeight)
            {
                MessageHub.Instance.Publish(new SyncUnfinishedBlock(currentHeight + 1));
            }
            
            switch (message.BlockValidationResult)
            {
                case BlockValidationResult.Pending: break;
                case BlockValidationResult.AlreadyExecuted: break;
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