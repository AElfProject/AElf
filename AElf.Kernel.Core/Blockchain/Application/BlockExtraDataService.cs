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

        //TODO: Add FillBlockExtraData test case [Case]
        public async Task FillBlockExtraData(BlockHeader blockHeader)
        {
            foreach (var blockExtraDataProvider in _blockExtraDataProviders)
            {
                var extraData = await blockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(blockHeader);
                if (extraData != null) 
                {
                    // Actually extraData cannot be NULL if it is mining processing, as the index in BlockExtraData is fixed.
                    // So it can be ByteString.Empty but not NULL.
                    blockHeader.BlockExtraDatas.Add(extraData);
                }
            }
        }

        public ByteString GetExtraDataFromBlockHeader(string blockExtraDataProviderSymbol, BlockHeader blockHeader)
        {
            for (var i = 0; i < _blockExtraDataProviders.Count; i++)
            {
                var blockExtraDataProviderName = _blockExtraDataProviders[i].GetType().Name;
                if (blockExtraDataProviderName.Contains(blockExtraDataProviderSymbol) && i < blockHeader.BlockExtraDatas.Count)
                {
                    return blockHeader.BlockExtraDatas[i];
                }
            }

            return null;
        }
    }
}