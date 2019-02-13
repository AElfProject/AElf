using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.BlockService
{
    public class BlockExtraDataService : IBlockExtraDataService
    {
        private readonly List<IBlockExtraDataProvider> _blockExtraDataProviders;

        public BlockExtraDataService(List<IBlockExtraDataProvider> blockExtraDataProviders)
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