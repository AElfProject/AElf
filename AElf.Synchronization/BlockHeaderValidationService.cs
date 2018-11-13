using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;

namespace AElf.Synchronization
{
    public class BlockHeaderValidationService : IBlockHeaderValidationService
    {
        private readonly IBlockSet _blockSet;

        private readonly IChainService _chainService;

        private IBlockChain _blockChain;

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(ChainConfig.Instance.ChainId)));

        public BlockHeaderValidationService(IBlockSet blockSet, IChainService chainService)
        {
            _blockSet = blockSet;
            _chainService = chainService;
        }

        public Task<bool> CheckLinkabilityAsync(BlockHeader blockHeader)
        {
            var previousBlocks = _blockSet.GetBlockByHeight(blockHeader.Index - 1);
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
            var localPreviousBlock = await BlockChain.GetBlockByHeightAsync(blockHeader.Index - 1);
            if (localPreviousBlock == null)
            {
                return BlockHeaderValidationResult.FutureBlock;
            }

            if (localPreviousBlock.BlockHashToHex != blockHeader.PreviousBlockHash.DumpHex())
            {
                return BlockHeaderValidationResult.Branched;
            }

            if (await CheckLinkabilityAsync(blockHeader))
            {
                return BlockHeaderValidationResult.Unlinkable;
            }

            return BlockHeaderValidationResult.Success;
        }
    }
}