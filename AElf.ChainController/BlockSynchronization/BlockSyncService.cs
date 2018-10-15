using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Easy.MessageHub;

namespace AElf.ChainController
{
    public class BlockSyncService : IBlockSyncService
    {
        private readonly IChainService _chainService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IBlockExecutionService _blockExecutionService;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;

        private readonly IBlockCollection _blockCollection = new BlockCollection();

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(NodeConfig.Instance.ChainId)));

        public BlockSyncService(IChainService chainService, IBlockValidationService blockValidationService,
            IBlockExecutionService blockExecutionService, IBinaryMerkleTreeManager binaryMerkleTreeManager)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            _blockExecutionService = blockExecutionService;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
        }

        public async Task ReceiveBlock(IBlock block)
        {
            var blockValidationResult =
                await _blockValidationService.ValidateBlockAsync(block, await GetChainContextAsync());

            var message = new BlockAccepted(block, blockValidationResult);
            MessageHub.Instance.Publish(message);

            if (blockValidationResult == BlockValidationResult.Success)
            {
                await HandleValidBlock(block);
            }
            else
            {
                await HandleInvalidBlock(message);
            }
        }

        public async Task AddMinedBlock(IBlock block)
        {
            await _blockCollection.Tell(block.Header.Index);
        }

        private async Task HandleValidBlock(IBlock block)
        {
            var executionResult = await _blockExecutionService.ExecuteBlock(block);
            if (executionResult == BlockExecutionResultCC.Success)
            {
                await _blockCollection.Tell(block.Header.Index);
            }
            else
            {
                await BlockChain.AddBlocksAsync(new List<IBlock> {block});
                await _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree,
                    block.Header.ChainId,
                    block.Header.Index);
                await _binaryMerkleTreeManager.AddSideChainTransactionRootsMerkleTreeAsync(
                    block.Body.BinaryMerkleTreeForSideChainTransactionRoots, block.Header.ChainId, block.Header.Index);
            }
        }

        // TODO: Very important. Need to redesign the validation results.
        private async Task HandleInvalidBlock(BlockAccepted message)
        {
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