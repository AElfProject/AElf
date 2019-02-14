using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;

namespace AElf.Synchronization
{
    public class BlockHeaderValidator : IBlockHeaderValidator
    {
        private readonly IChainService _chainService;

        private IBlockChain _blockChain;

        public BlockHeaderValidator(IChainService chainService)
        {
            _chainService = chainService;
        }

        public async Task<BlockHeaderValidationResult> ValidateBlockHeaderAsync(int chainId, BlockHeader blockHeader)
        {
            if (_blockChain == null)
            {
                _blockChain =_chainService.GetBlockChain(chainId);
            }

            var currentHeight = await _blockChain.GetCurrentBlockHeightAsync();

            if (blockHeader.Height == currentHeight + 1)
            {
                var localCurrentBlock = await _blockChain.GetBlockByHeightAsync(currentHeight);
                if (localCurrentBlock.BlockHashToHex != blockHeader.PreviousBlockHash.ToHex())
                {
                    MessageHub.Instance.Publish(new BranchedBlockReceived());
                    return BlockHeaderValidationResult.Branched;
                }

                return BlockHeaderValidationResult.Success;
            }
            
            var localBlock = await _blockChain.GetBlockByHeightAsync(blockHeader.Height);
            if (localBlock != null && localBlock.BlockHashToHex == blockHeader.GetHash().ToHex())
            {
                return BlockHeaderValidationResult.AlreadyExecuted;
            }

            MessageHub.Instance.Publish(new BranchedBlockReceived());
            return BlockHeaderValidationResult.Branched;
        }
    }
}