using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public class BlockExtraDataService : IBlockExtraDataService
    {
        private readonly IEnumerable<IBlockExtraDataProvider> _blockExtraDataProviders;

        public BlockExtraDataService(IEnumerable<IBlockExtraDataProvider> blockExtraDataProviders)
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