using System.Threading.Tasks;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Txn.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Application
{
    internal class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
        private readonly EvilTriggerOptions _evilTriggerOptions;
        public string BlockHeaderExtraDataKey => CrossChainConstants.CrossChainExtraDataKey;
        public ILogger<CrossChainBlockExtraDataProvider> Logger { get; set; }

        public CrossChainBlockExtraDataProvider(ICrossChainIndexingDataService crossChainIndexingDataService,
            ITransactionPackingOptionProvider transactionPackingOptionProvider,
            IOptionsMonitor<EvilTriggerOptions> evilTriggerOptions)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _transactionPackingOptionProvider = transactionPackingOptionProvider;
            _evilTriggerOptions = evilTriggerOptions.CurrentValue;
            Logger = NullLogger<CrossChainBlockExtraDataProvider>.Instance;
        }

        public async Task<ByteString> GetBlockHeaderExtraDataAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == AElfConstants.GenesisBlockHeight)
                return ByteString.Empty;

            if (!_transactionPackingOptionProvider.IsTransactionPackable(new ChainContext
                {BlockHash = blockHeader.PreviousBlockHash, BlockHeight = blockHeader.Height - 1}))
                return ByteString.Empty;

            var bytes = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(
                blockHeader.PreviousBlockHash, blockHeader.Height - 1);

            if (_evilTriggerOptions.ErrorCrossChainExtraDate &&
                blockHeader.Height % _evilTriggerOptions.EvilTriggerNumber == 0)
            {
                if (bytes.Equals(ByteString.Empty))
                {
                    var fake = "FakeCrossChainExtraDate";
                    bytes = ByteString.CopyFrom(
                        ByteArrayHelper.HexStringToByteArray(HashHelper.ComputeFrom(fake).ToHex()));
                    Logger.LogWarning(
                        $"EVIL TRIGGER - ErrorCrossChainExtraDate - Empty to FakeCrossChainExtraDate");
                }
                else
                {
                    bytes = ByteString.Empty;
                    Logger.LogWarning(
                        $"EVIL TRIGGER - ErrorCrossChainExtraDate - to Empty");
                }
            }

            return bytes;
        }
    }
}