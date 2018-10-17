using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Easy.MessageHub;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IChainService _chainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockExecutionService _blockExecutionService;

        private readonly IBlockSet _blockSet = new BlockSet();

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(NodeConfig.Instance.ChainId)));

        public BlockSyncService(IChainService chainService, IBlockValidationService blockValidationService,
            IBlockExecutionService blockExecutionService)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            _blockExecutionService = blockExecutionService;
        }

        public async Task ReceiveBlock(IBlock block)
        {
            var blockValidationResult =
                await _blockValidationService.ValidateBlockAsync(block, await GetChainContextAsync());

            var message = new BlockAccepted(block, blockValidationResult);

            if (blockValidationResult == BlockValidationResult.Success)
            {
                await HandleValidBlock(message);
            }
            else
            {
                await HandleInvalidBlock(message);
            }
        }

        public async Task AddMinedBlock(IBlock block)
        {
            await _blockSet.Tell(block.Header.Index);
            MessageHub.Instance.Publish(UpdateConsensus.Update);
        }

        private async Task HandleValidBlock(BlockAccepted message)
        {
            await _blockSet.AddBlock(message.Block);
            var executionResult = await _blockExecutionService.ExecuteBlock(message.Block);
            if (executionResult == BlockExecutionResultCC.Success)
            {
                await _blockSet.Tell(message.Block.Header.Index);
                MessageHub.Instance.Publish(message);
                MessageHub.Instance.Publish(UpdateConsensus.Update);
            }
            else
            {
                // rollback
            }
        }

        // TODO: Very important. Need to redesign the validation results.
        private async Task HandleInvalidBlock(BlockAccepted message)
        {
            await _blockSet.AddBlock(message.Block);
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