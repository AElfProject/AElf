using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;
using NLog;

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

        private readonly ILogger _logger = LogManager.GetLogger(nameof(BlockHeaderValidator));

        public BlockHeaderValidator(IBlockSet blockSet, IChainService chainService)
        {
            _blockSet = blockSet;
            _chainService = chainService;
        }

        public async Task<bool> CheckLinkabilityAsync(BlockHeader blockHeader)
        {
            var previousBlocks = new List<IBlock> {await BlockChain.GetBlockByHeightAsync(blockHeader.Index - 1)};
            previousBlocks.AddRange(_blockSet.GetBlocksByHeight(blockHeader.Index - 1));
            foreach (var previousBlock in previousBlocks)
            {
                if (previousBlock.BlockHashToHex == blockHeader.PreviousBlockHash.DumpHex())
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<BlockHeaderValidationResult> ValidateBlockHeaderAsync(BlockHeader blockHeader)
        {
            var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();
            
            if (blockHeader.Index > currentHeight + 1)
            {
                if (blockHeader.Index >= currentHeight + GlobalConfig.ForkDetectionLength)
                {
                    return BlockHeaderValidationResult.MaybeForked;
                }
                return BlockHeaderValidationResult.FutureBlock;
            }

            // Step 1: Check linkability to any of local blocks (contains block cache).
            // Step 2: Check whether this block in branched chain.
            if (blockHeader.Index == currentHeight + 1)
            {
                if (!await CheckLinkabilityAsync(blockHeader))
                {
                    return BlockHeaderValidationResult.Unlinkable;
                }
                
                var localCurrentBlock = await BlockChain.GetBlockByHeightAsync(currentHeight);
                if (localCurrentBlock.BlockHashToHex != blockHeader.PreviousBlockHash.DumpHex())
                {
                    return BlockHeaderValidationResult.Branched;
                }

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