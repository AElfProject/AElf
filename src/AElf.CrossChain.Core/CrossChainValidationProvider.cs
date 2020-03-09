using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain
{
    public class CrossChainValidationProvider : IBlockValidationProvider
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<CrossChainValidationProvider> Logger { get; set; }
        
        public CrossChainValidationProvider(ICrossChainIndexingDataService crossChainIndexingDataService, 
            IBlockExtraDataService blockExtraDataService, ISmartContractAddressService smartContractAddressService)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _blockExtraDataService = blockExtraDataService;
            _smartContractAddressService = smartContractAddressService;
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
            if (block.Header.Height == Constants.GenesisBlockHeight)
                return true;

            try
            {
                var isSideChainBlockDataIndexed = TryDiscoverIndexedSideChainBlockData(block);
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
                        await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(block.Header.GetHash(), block.Header.Height);
                    if (indexedCrossChainBlockData.IsNullOrEmpty() ^ extraData.IsNullOrEmpty())
                        validationResult = false;
                    else if (!indexedCrossChainBlockData.IsNullOrEmpty())
                        validationResult = ValidateBlockExtraDataAsync(indexedCrossChainBlockData, extraData);
                }                
                 
                if (!validationResult)
                    Logger.LogWarning(
                        $"Invalid cross chain extra data, block height {block.Header.Height}, hash {block.GetHash()}.");
                return validationResult;
            }
            finally
            {
                await LocalEventBus.PublishAsync(new CrossChainDataValidatedEvent());
            }
        }

        private bool ValidateBlockExtraDataAsync(CrossChainBlockData crossChainBlockData, ByteString extraData)
        {
            var expected =
                _crossChainIndexingDataService.ExtractCrossChainExtraDataFromCrossChainBlockData(crossChainBlockData);
            return expected.Equals(extraData);
        }

        private ByteString ExtractCrossChainExtraData(BlockHeader header)
        {
            var bytes = _blockExtraDataService.GetExtraDataFromBlockHeader(CrossChainConstants.CrossChainExtraDataNamePrefix, header);
            return bytes;
        }
        
        private bool TryDiscoverIndexedSideChainBlockData(IBlock block)
        {
            var crossChainContractAddress =
                _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);
            return new SideChainBlockDataIndexed().ToLogEvent(crossChainContractAddress).GetBloom()
                .IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }
    }
}