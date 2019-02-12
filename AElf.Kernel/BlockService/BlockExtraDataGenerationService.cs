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

        public async Task FillBlockExtraData(Block block)
        {
            foreach (var blockExtraDataProvider in _blockExtraDataProviders)
            {
                await blockExtraDataProvider.FillExtraData(block);
            }
        }
    }
}