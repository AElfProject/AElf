using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockExtraDataService : IBlockExtraDataService
    {
        private readonly List<IBlockExtraDataProvider> _blockExtraDataProviders;

        public BlockExtraDataService(IEnumerable<IBlockExtraDataProvider> blockExtraDataProviders)
        {
            _blockExtraDataProviders = blockExtraDataProviders.ToList();
        }

        public async Task FillBlockExtraDataAsync(BlockHeader blockHeader)
        {
            foreach (var blockExtraDataProvider in _blockExtraDataProviders)
            {
                var extraData = await blockExtraDataProvider.GetBlockHeaderExtraDataAsync(blockHeader);
                if (extraData != null)
                {
                    // Actually extraData cannot be NULL if it is mining processing, as the index in BlockExtraData is fixed.
                    // So it can be ByteString.Empty but not NULL.
                    blockHeader.ExtraData.Add(blockExtraDataProvider.BlockHeaderExtraDataKey, extraData);
                }
            }
        }

        public ByteString GetExtraDataFromBlockHeader(string blockHeaderExtraDataKey, BlockHeader blockHeader)
        {
            if (blockHeader.Height == AElfConstants.GenesisBlockHeight)
                return null;

            return blockHeader.ExtraData.TryGetValue(blockHeaderExtraDataKey, out var extraData)
                ? extraData
                : null;
        }
    }
}