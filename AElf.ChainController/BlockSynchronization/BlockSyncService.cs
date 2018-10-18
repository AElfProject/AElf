using System.Collections.Generic;
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
                _logger?.Trace($"Block {block.GetHash().DumpHex()} is a invalid block.");
                await HandleInvalidBlock(message);
            }

            return blockValidationResult;
        }

        public async Task AddMinedBlock(IBlock block)
        {
            _blockSet.AddBlock(block);
            _blockSet.Tell(block.Header.Index);
            MessageHub.Instance.Publish(UpdateConsensus.Update);
            
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
                
                // Find new blocks from block set to execute
                var blocks = _blockSet.GetBlockByHeight(message.Block.Header.Index + 1);
                if (blocks.Count > 0)
                {
                    _logger?.Trace("Will get block from block set to execute.");
                    foreach (var block in blocks)
                    {
                        if (await ReceiveBlock(block) == BlockValidationResult.Success)
                        {
                            break;
                        }
                    }
                }
            }
            // TODO: else
        }

        // TODO: Very important. Need to redesign the validation results.
        private async Task HandleInvalidBlock(BlockAccepted message)
        {
            _blockSet.AddBlock(message.Block);
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