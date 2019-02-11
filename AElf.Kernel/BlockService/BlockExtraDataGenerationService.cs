using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.BlockService
{
    public class BlockExtraDataGenerationService : IBlockExtraDataGenerationService
    {
        private readonly List<IBlockExtraDataProvider> _blockExtraDataProviders;

        public BlockExtraDataGenerationService(List<IBlockExtraDataProvider> blockExtraDataProviders)
        {
            _blockExtraDataProviders = blockExtraDataProviders;
        }

        public async Task AddBlockExtraData(Block block)
        {
            block.Header.BlockExtraData = new BlockExtraData();
            foreach (var blockExtraDataProvider in _blockExtraDataProviders)
            {
                await blockExtraDataProvider.TryAddExtraData(block);
            }
        }
    }
}