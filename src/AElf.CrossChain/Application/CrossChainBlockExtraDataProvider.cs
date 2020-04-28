using System.Threading.Tasks;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Txn.Application;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Application
{
    internal class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly TransactionPackingOptions _transactionPackingOptions;
        public string BlockHeaderExtraDataKey => CrossChainConstants.CrossChainExtraDataKey;

        public CrossChainBlockExtraDataProvider(ICrossChainIndexingDataService crossChainIndexingDataService,
            IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;
        }

        public async Task<ByteString> GetBlockHeaderExtraDataAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == AElfConstants.GenesisBlockHeight)
                return ByteString.Empty;

            if (!_transactionPackingOptions.IsTransactionPackable)
                return ByteString.Empty;

            var bytes = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(
                blockHeader.PreviousBlockHash, blockHeader.Height - 1);

            return bytes;
        }
    }
}