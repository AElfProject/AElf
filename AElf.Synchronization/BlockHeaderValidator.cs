using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;

namespace AElf.Synchronization
{
    public class BlockHeaderValidator : IBlockHeaderValidator
    {
        private readonly IBlockSet _blockSet;

        private readonly IChainService _chainService;

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(ChainConfig.Instance.ChainId)));

        private ulong _latestValidHeight = 1;
        
        public BlockHeaderValidator(IBlockSet blockSet, IChainService chainService)
        {
            _blockSet = blockSet;
            _chainService = chainService;
        }

        public Task<bool> CheckLinkabilityAsync(BlockHeader blockHeader)
        {
            var previousBlocks = _blockSet.GetBlocksByHeight(blockHeader.Index - 1);
            foreach (var previousBlock in previousBlocks)
            {
                if (previousBlock.BlockHashToHex == blockHeader.PreviousBlockHash.DumpHex())
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public async Task<BlockHeaderValidationResult> ValidateBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Index > _latestValidHeight + 1)
            {
                return BlockHeaderValidationResult.FutureBlock;
            }

            // Step 1: Check linkability to any of local blocks (contains block cache).
            // Step 2: Check whether this block in branched chain.
            if (blockHeader.Index == _latestValidHeight + 1)
            {
                var localCurrentBlock = await BlockChain.GetBlockByHeightAsync(_latestValidHeight);
                
                if (!await CheckLinkabilityAsync(blockHeader))
                {
                    return BlockHeaderValidationResult.Unlinkable;
                }
                
                if (localCurrentBlock.BlockHashToHex != blockHeader.PreviousBlockHash.DumpHex())
                {
                    return BlockHeaderValidationResult.Branched;
                }

                _latestValidHeight = blockHeader.Index;
                return BlockHeaderValidationResult.Success;
            }
            
            var localBlock = await BlockChain.GetBlockByHeightAsync(blockHeader.Index - 1);
            if (localBlock.BlockHashToHex == blockHeader.GetHash().DumpHex())
            {
                return BlockHeaderValidationResult.AlreadyExecuted;
            }

            return BlockHeaderValidationResult.Branched;
        }
    }
}