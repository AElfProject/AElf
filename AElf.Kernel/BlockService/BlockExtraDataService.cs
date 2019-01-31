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


        public Task<byte[]> GenerateExtraData()
        {
            throw new System.NotImplementedException();
        }
    }
}