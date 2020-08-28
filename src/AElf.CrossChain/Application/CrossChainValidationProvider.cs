using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Local;
using AElf.CSharp.Core.Extension;

namespace AElf.CrossChain.Application
{
    public class CrossChainValidationProvider : IBlockValidationProvider
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly ICrossChainRequestService _crossChainRequestService;
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<CrossChainValidationProvider> Logger { get; set; }

        public CrossChainValidationProvider(ICrossChainIndexingDataService crossChainIndexingDataService,
            IBlockExtraDataService blockExtraDataService, ISmartContractAddressService smartContractAddressService,
            ICrossChainRequestService crossChainRequestService)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _blockExtraDataService = blockExtraDataService;
            _smartContractAddressService = smartContractAddressService;
            _crossChainRequestService = crossChainRequestService;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            var extraData = ExtractCrossChainExtraData(block.Header);
            if (!extraData.IsNullOrEmpty())
                return await _crossChainIndexingDataService.CheckExtraDataIsNeededAsync(block.Header.PreviousBlockHash,
                    block.Header.Height - 1, block.Header.Time); 
            return true;
        }

        public Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            if (block.Header.Height == AElfConstants.GenesisBlockHeight)
                return true;

            try
            {
                var isSideChainBlockDataIndexed = await TryDiscoverIndexedSideChainBlockDataAsync(block);
                Logger.LogDebug($"Try discovery indexed side chain block data: {isSideChainBlockDataIndexed}");
                var extraData = ExtractCrossChainExtraData(block.Header);
                var validationResult = true;
                if (!isSideChainBlockDataIndexed && !extraData.IsNullOrEmpty())
                {
                    // cross chain extra data in block header should be null if no side chain block data indexed in contract 
                    validationResult = false;
                }
                else if (isSideChainBlockDataIndexed)
                {
                    var indexedCrossChainBlockData =
                        await _crossChainIndexingDataService.GetIndexedSideChainBlockDataAsync(block.Header.GetHash(),
                            block.Header.Height);
                    if (indexedCrossChainBlockData.IsNullOrEmpty() ^ extraData.IsNullOrEmpty())
                        validationResult = false;
                    else if (!indexedCrossChainBlockData.IsNullOrEmpty())
                        validationResult = ValidateBlockExtraDataAsync(indexedCrossChainBlockData, extraData);
                }

                if (!validationResult)
                    Logger.LogDebug(
                        $"Invalid cross chain extra data, block height {block.Header.Height}, hash {block.GetHash()}.");
                return validationResult;
            }
            finally
            {
                _ = _crossChainRequestService.RequestCrossChainDataFromOtherChainsAsync();
            }
        }

        private bool ValidateBlockExtraDataAsync(IndexedSideChainBlockData indexedSideChainBlockData, ByteString extraData)
        {
            var expected = indexedSideChainBlockData.ExtractCrossChainExtraDataFromCrossChainBlockData();
            return expected.Equals(extraData);
        }

        private ByteString ExtractCrossChainExtraData(BlockHeader header)
        {
            var bytes = _blockExtraDataService.GetExtraDataFromBlockHeader(
                CrossChainConstants.CrossChainExtraDataKey, header);
            return bytes;
        }

        private async Task<bool> TryDiscoverIndexedSideChainBlockDataAsync(IBlock block)
        {
            var crossChainContractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Header.Height
                }, CrossChainSmartContractAddressNameProvider.StringName);
            return new SideChainBlockDataIndexed().ToLogEvent(crossChainContractAddress).GetBloom()
                .IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }
    }
}