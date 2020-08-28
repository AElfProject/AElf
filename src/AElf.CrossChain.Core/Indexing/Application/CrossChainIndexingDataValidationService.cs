using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache.Application;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Indexing.Application
{
    internal class CrossChainIndexingDataValidationService : ICrossChainIndexingDataValidationService,
        ITransientDependency
    {
        public ILogger<CrossChainIndexingDataValidationService> Logger { get; set; }
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;

        private readonly IContractReaderFactory<CrossChainContractImplContainer.CrossChainContractImplStub>
            _contractReaderFactory;

        private readonly ISmartContractAddressService _smartContractAddressService;

        private Task<Address> GetCrossChainContractAddressAsync(IChainContext chainContext)
        {
            return _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                CrossChainSmartContractAddressNameProvider.StringName);
        }

        public CrossChainIndexingDataValidationService(IBlockCacheEntityConsumer blockCacheEntityConsumer,
            IContractReaderFactory<CrossChainContractImplContainer.CrossChainContractImplStub> contractReaderFactory,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockCacheEntityConsumer = blockCacheEntityConsumer;
            _contractReaderFactory = contractReaderFactory;
            _smartContractAddressService = smartContractAddressService;
        }


        public async Task<bool> ValidateCrossChainIndexingDataAsync(CrossChainBlockData crossChainBlockData,
            Hash blockHash, long blockHeight)
        {
            var sideChainBlockDataValidationResult =
                await ValidateSideChainBlockDataAsync(crossChainBlockData.SideChainBlockDataList, blockHash,
                    blockHeight);
            if (!sideChainBlockDataValidationResult)
                return false;

            var parentChainBlockDataValidationResult =
                await ValidateParentChainBlockDataAsync(crossChainBlockData.ParentChainBlockDataList, blockHash,
                    blockHeight);

            return parentChainBlockDataValidationResult;
        }


        private async Task<bool> ValidateSideChainBlockDataAsync(
            IEnumerable<SideChainBlockData> multiSideChainBlockData,
            Hash blockHash, long blockHeight)
        {
            var sideChainValidatedHeightDict = new Dictionary<int, long>(); // chain id => validated height
            foreach (var sideChainBlockData in multiSideChainBlockData)
            {
                if (!sideChainValidatedHeightDict.TryGetValue(sideChainBlockData.ChainId, out var validatedHeight))
                {
                    var height = await _contractReaderFactory
                        .Create(new ContractReaderContext
                        {
                            BlockHash = blockHash,
                            BlockHeight = blockHeight,
                            ContractAddress = await GetCrossChainContractAddressAsync(new ChainContext
                            {
                                BlockHash = blockHash,
                                BlockHeight = blockHeight
                            })
                        }).GetSideChainHeight
                        .CallAsync(
                            new Int32Value()
                            {
                                Value = sideChainBlockData.ChainId
                            });
                    validatedHeight = height?.Value ?? 0;
                }

                var targetHeight = validatedHeight + 1;

                if (targetHeight != sideChainBlockData.Height)
                    // this should not happen if it is good data.
                    return false;

                var cachedSideChainBlockData =
                    _blockCacheEntityConsumer.Take<SideChainBlockData>(sideChainBlockData.ChainId, targetHeight, false);
                if (cachedSideChainBlockData == null)
                {
                    Logger.LogDebug(
                        $"Side chain data not found. ChainId: {ChainHelper.ConvertChainIdToBase58(sideChainBlockData.ChainId)}, side chain height: {targetHeight}.");
                    return false;
                }

                if (!cachedSideChainBlockData.Equals(sideChainBlockData))
                {
                    Logger.LogDebug(
                        $"Incorrect side chain data. ChainId: {ChainHelper.ConvertChainIdToBase58(sideChainBlockData.ChainId)}, side chain height: {targetHeight}.");
                    return false;
                }

                sideChainValidatedHeightDict[sideChainBlockData.ChainId] = sideChainBlockData.Height;
            }

            return true;
        }

        private async Task<bool> ValidateParentChainBlockDataAsync(
            IEnumerable<ParentChainBlockData> multiParentChainBlockData,
            Hash blockHash, long blockHeight)
        {
            var parentChainBlockDataList = multiParentChainBlockData.ToList();
            if (parentChainBlockDataList.Count == 0)
                return true;
            var crossChainContractAddress = await GetCrossChainContractAddressAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
            var parentChainId = (await _contractReaderFactory
                .Create(new ContractReaderContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight,
                    ContractAddress = crossChainContractAddress
                }).GetParentChainId
                .CallAsync(new Empty())).Value;
            if (parentChainId == 0)
                // no configured parent chain
                return false;

            var length = parentChainBlockDataList.Count;
            var i = 0;
            var targetHeight = (await _contractReaderFactory.Create(new ContractReaderContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight,
                    ContractAddress = crossChainContractAddress
                }).GetParentChainHeight
                .CallAsync(new Empty())).Value + 1;
            while (i < length)
            {
                var parentChainBlockData =
                    _blockCacheEntityConsumer.Take<ParentChainBlockData>(parentChainId, targetHeight, false);
                if (parentChainBlockData == null)
                {
                    Logger.LogDebug(
                        $"Parent chain data not found. Parent chain height: {targetHeight}.");
                    return false;
                }

                if (!parentChainBlockDataList[i].Equals(parentChainBlockData))
                {
                    Logger.LogDebug(
                        $"Incorrect parent chain data. Parent chain height: {targetHeight}.");
                    return false;
                }

                targetHeight++;
                i++;
            }

            return true;
        }
    }
}