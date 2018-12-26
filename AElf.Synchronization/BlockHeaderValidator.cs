using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
namespace AElf.Synchronization
{
    public class BlockHeaderValidator : IBlockHeaderValidator
    {
        private readonly IChainService _chainService;

        private IBlockChain _blockChain;
        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadBase58(ChainConfig.Instance.ChainId)));

        public BlockHeaderValidator(IChainService chainService)
        {
            _chainService = chainService;
        }

        public async Task<BlockHeaderValidationResult> ValidateBlockHeaderAsync(BlockHeader blockHeader)
        {
            var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();

            // Step 1: Check linkability to any of local blocks (contains block cache).
            // Step 2: Check whether this block in branched chain.
            if (blockHeader.Index == currentHeight + 1)
            {
//                if (!await CheckLinkabilityAsync(blockHeader))
//                {
//                    return BlockHeaderValidationResult.Unlinkable;
//                }
                
                var localCurrentBlock = await BlockChain.GetBlockByHeightAsync(currentHeight);
                if (localCurrentBlock.BlockHashToHex != blockHeader.PreviousBlockHash.ToHex())
                {
                    MessageHub.Instance.Publish(new BranchedBlockReceived());
                    return BlockHeaderValidationResult.Branched;
                }

                return BlockHeaderValidationResult.Success;
            }
            
            var localBlock = await BlockChain.GetBlockByHeightAsync(blockHeader.Index);
            if (localBlock != null && localBlock.BlockHashToHex == blockHeader.GetHash().ToHex())
            {
                return BlockHeaderValidationResult.AlreadyExecuted;
            }

            MessageHub.Instance.Publish(new BranchedBlockReceived());
            return BlockHeaderValidationResult.Branched;
        }
    }
}