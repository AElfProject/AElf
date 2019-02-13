using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
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
                                                  ChainConfig.Instance.ChainId.ConvertBase58ToChainId()));

        public BlockHeaderValidator(IChainService chainService)
        {
            _chainService = chainService;
        }

        public async Task<BlockHeaderValidationResult> ValidateBlockHeaderAsync(BlockHeader blockHeader)
        {
            var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();

            if (blockHeader.Height == currentHeight + 1)
            {
                var localCurrentBlock = await BlockChain.GetBlockByHeightAsync(currentHeight);
                if (localCurrentBlock.BlockHashToHex != blockHeader.PreviousBlockHash.ToHex())
                {
                    MessageHub.Instance.Publish(new BranchedBlockReceived());
                    return BlockHeaderValidationResult.Branched;
                }

                return BlockHeaderValidationResult.Success;
            }
            
            var localBlock = await BlockChain.GetBlockByHeightAsync(blockHeader.Height);
            if (localBlock != null && localBlock.BlockHashToHex == blockHeader.GetHash().ToHex())
            {
                return BlockHeaderValidationResult.AlreadyExecuted;
            }

            MessageHub.Instance.Publish(new BranchedBlockReceived());
            return BlockHeaderValidationResult.Branched;
        }
    }
}