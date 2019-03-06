using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockExtraDataService : IBlockExtraDataService
    {
        private readonly IEnumerable<IBlockExtraDataProvider> _blockExtraDataProviders;

        public BlockExtraDataService(IEnumerable<IBlockExtraDataProvider> blockExtraDataProviders)
        {
            _blockExtraDataProviders = blockExtraDataProviders;
        }

        public async Task FillBlockExtraData(BlockHeader blockHeader)
        {
            foreach (var blockExtraDataProvider in _blockExtraDataProviders)
            {
                blockHeader.BlockExtraDatas.Add(await blockExtraDataProvider.FillExtraDataAsync(blockHeader));
            }
        }
    }
}