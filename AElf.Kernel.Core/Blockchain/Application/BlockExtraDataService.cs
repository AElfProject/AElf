using System;
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

        public async Task FillBlockExtraData(BlockHeader blockHeader)
        {
            foreach (var blockExtraDataProvider in _blockExtraDataProviders)
            {
                var extraData = await blockExtraDataProvider.GetExtraDataAsync(blockHeader);
                if (extraData != null)
                {
                    blockHeader.BlockExtraDatas.Add(extraData);
                }
            }
        }

        public ByteString GetBlockExtraData(Type blockExtraDataProviderType, BlockHeader blockHeader)
        {
            for (var i = 0; i < _blockExtraDataProviders.Count; i++)
            {
                if (_blockExtraDataProviders[i].GetType() == blockExtraDataProviderType)
                {
                    return blockHeader.BlockExtraDatas[i];
                }
            }

            return null;
        }
    }
}