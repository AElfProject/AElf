using System.Threading.Tasks;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;

namespace AElf.CrossChain
{
    internal class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        
        public CrossChainBlockExtraDataProvider(ICrossChainIndexingDataService crossChainIndexingDataService)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == Constants.GenesisBlockHeight)
                return ByteString.Empty;

            var bytes = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(
                blockHeader.PreviousBlockHash, blockHeader.Height - 1);
            
            return bytes;
        }
    }
}