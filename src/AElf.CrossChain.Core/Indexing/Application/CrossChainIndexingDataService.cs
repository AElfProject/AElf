using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Indexing.Application
{
    internal class CrossChainIndexingDataService : ICrossChainIndexingDataService
    {
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;
        private readonly ITransactionInputForBlockMiningDataProvider _transactionInputForBlockMiningDataProvider;
        private readonly IIrreversibleBlockStateProvider _irreversibleBlockStateProvider;

        private readonly IContractReaderFactory<CrossChainContractImplContainer.CrossChainContractImplStub>
            _contractReaderFactory;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<CrossChainIndexingDataService> Logger { get; set; }

        private Task<Address> GetCrossChainContractAddressAsync(IChainContext chainContext)
        {
            return _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                CrossChainSmartContractAddressNameProvider.StringName);
        }


        public CrossChainIndexingDataService(IBlockCacheEntityConsumer blockCacheEntityConsumer,
            ITransactionInputForBlockMiningDataProvider transactionInputForBlockMiningDataProvider,
            IIrreversibleBlockStateProvider irreversibleBlockStateProvider,
            IContractReaderFactory<CrossChainContractImplContainer.CrossChainContractImplStub> contractReaderFactory,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockCacheEntityConsumer = blockCacheEntityConsumer;
            _transactionInputForBlockMiningDataProvider = transactionInputForBlockMiningDataProvider;
            _irreversibleBlockStateProvider = irreversibleBlockStateProvider;
            _contractReaderFactory = contractReaderFactory;
            _smartContractAddressService = smartContractAddressService;
        }


        private async Task<List<SideChainBlockData>> GetNonIndexedSideChainBlockDataAsync(Hash blockHash,
            long blockHeight, HashSet<int> excludeChainIdList)
        {
            var crossChainContractAddress = await GetCrossChainContractAddressAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
            var sideChainBlockDataList = new List<SideChainBlockData>();
            var sideChainIndexingInformationList = await _contractReaderFactory
                .Create(new ContractReaderContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight,
                    ContractAddress = crossChainContractAddress
                })
                .GetSideChainIndexingInformationList.CallAsync(new Empty());
            foreach (var sideChainIndexingInformation in sideChainIndexingInformationList.IndexingInformationList)
            {
                var libDto = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
                var sideChainId = sideChainIndexingInformation.ChainId;
                if (excludeChainIdList.Contains(sideChainId))
                    continue;
                var sideChainHeightInLibValue = await _contractReaderFactory
                    .Create(new ContractReaderContext
                    {
                        BlockHash = libDto.BlockHash,
                        BlockHeight = libDto.BlockHeight,
                        ContractAddress = crossChainContractAddress
                    })
                    .GetSideChainHeight.CallAsync(new Int32Value {Value = sideChainId});

                long toBeIndexedCount;
                long targetHeight;
                var sideChainHeightInLib = sideChainHeightInLibValue?.Value ?? 0;
                if (sideChainHeightInLib > 0)
                {
                    targetHeight = sideChainIndexingInformation.IndexedHeight + 1;
                    toBeIndexedCount = CrossChainConstants.DefaultBlockCacheEntityCount;
                    Logger.LogDebug(
                        $"Target height {targetHeight} of side chain " +
                        $"{ChainHelper.ConvertChainIdToBase58(sideChainId)}.");
                }
                else if (sideChainIndexingInformation.IndexedHeight > 0)
                {
                    toBeIndexedCount = 0;
                    targetHeight = sideChainIndexingInformation.IndexedHeight + 1;
                }
                else
                {
                    toBeIndexedCount = 1;
                    targetHeight = AElfConstants.GenesisBlockHeight;
                    Logger.LogDebug(
                        $"Target height {targetHeight} of side chain " +
                        $"{ChainHelper.ConvertChainIdToBase58(sideChainId)}.");
                }

                var sideChainBlockDataFromCache = new List<SideChainBlockData>();

                var i = 0;
                while (i < toBeIndexedCount)
                {
                    var sideChainBlockData =
                        _blockCacheEntityConsumer.Take<SideChainBlockData>(sideChainIndexingInformation.ChainId,
                            targetHeight, targetHeight == AElfConstants.GenesisBlockHeight);
                    if (sideChainBlockData == null || sideChainBlockData.Height != targetHeight)
                    {
                        // no more available side chain block info
                        break;
                    }

                    sideChainBlockDataFromCache.Add(sideChainBlockData);
                    targetHeight++;
                    i++;
                }

                if (sideChainBlockDataFromCache.Count > 0)
                {
                    Logger.LogDebug(
                        $"Got height [{sideChainBlockDataFromCache.First().Height} - {sideChainBlockDataFromCache.Last().Height} ]" +
                        $" from side chain {ChainHelper.ConvertChainIdToBase58(sideChainIndexingInformation.ChainId)}.");
                    sideChainBlockDataList.AddRange(sideChainBlockDataFromCache);
                }
            }

            return sideChainBlockDataList;
        }

        private async Task<List<ParentChainBlockData>> GetNonIndexedParentChainBlockDataAsync(Hash blockHash,
            long blockHeight, HashSet<int> excludeChainIdList)
        {
            var parentChainBlockDataList = new List<ParentChainBlockData>();
            var libExists = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            if (!libExists)
                return parentChainBlockDataList;

            var crossChainContractAddress = await GetCrossChainContractAddressAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });

            var returnValue = await _contractReaderFactory.Create(new ContractReaderContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight,
                    ContractAddress = crossChainContractAddress
                }).GetParentChainId
                .CallAsync(new Empty());
            var parentChainId = returnValue?.Value ?? 0;
            if (parentChainId == 0 || excludeChainIdList.Contains(parentChainId))
            {
                // no configured parent chain
                return parentChainBlockDataList;
            }

            int length = CrossChainConstants.DefaultBlockCacheEntityCount;
            var heightInState = (await _contractReaderFactory
                .Create(new ContractReaderContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight,
                    ContractAddress = crossChainContractAddress
                }).GetParentChainHeight
                .CallAsync(new Empty())).Value;

            var targetHeight = heightInState + 1;
            Logger.LogDebug($"Target height {targetHeight}");

            var i = 0;
            while (i < length)
            {
                var parentChainBlockData =
                    _blockCacheEntityConsumer.Take<ParentChainBlockData>(parentChainId, targetHeight, false);
                if (parentChainBlockData == null || parentChainBlockData.Height != targetHeight)
                {
                    // no more available parent chain block info
                    break;
                }

                parentChainBlockDataList.Add(parentChainBlockData);
                targetHeight++;
                i++;
            }

            if (parentChainBlockDataList.Count > 0)
                Logger.LogDebug(
                    $"Got height [{parentChainBlockDataList.First().Height} - {parentChainBlockDataList.Last().Height} ]" +
                    $" from parent chain {ChainHelper.ConvertChainIdToBase58(parentChainId)}.");
            return parentChainBlockDataList;
        }

        public async Task<IndexedSideChainBlockData> GetIndexedSideChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var indexedSideChainBlockData = await _contractReaderFactory
                .Create(new ContractReaderContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight,
                    ContractAddress = await GetCrossChainContractAddressAsync(new ChainContext
                    {
                        BlockHash = blockHash,
                        BlockHeight = blockHeight
                    })
                })
                .GetIndexedSideChainBlockDataByHeight.CallAsync(new Int64Value {Value = blockHeight});
            return indexedSideChainBlockData;
        }

        /// <summary>
        /// This method returns serialization input for cross chain proposing method.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public Task<CrossChainTransactionInput> GetCrossChainTransactionInputForNextMiningAsync(Hash blockHash,
            long blockHeight)
        {
            var inputForNextMining =
                _transactionInputForBlockMiningDataProvider.GetTransactionInputForBlockMining(blockHash);
            return Task.FromResult(inputForNextMining);
        }

        public async Task<bool> CheckExtraDataIsNeededAsync(Hash blockHash, long blockHeight, Timestamp timestamp)
        {
            var indexingProposalStatusList = await GetIndexingProposalStatusAsync(blockHash, blockHeight, timestamp);
            var toBeReleasedChainIdList = FindToBeReleasedChainIdList(indexingProposalStatusList, timestamp);
            return toBeReleasedChainIdList.Count > 0;
        }

        public async Task<ByteString> PrepareExtraDataForNextMiningAsync(Hash blockHash, long blockHeight)
        {
            var utcNow = TimestampHelper.GetUtcNow();
            var indexingProposalStatusList = await GetIndexingProposalStatusAsync(blockHash, blockHeight, utcNow);

            var toBeReleasedChainIdList = FindToBeReleasedChainIdList(indexingProposalStatusList, utcNow);

            if (toBeReleasedChainIdList.Count > 0)
            {
                // release pending proposal and unable to propose anything if it is ready
                _transactionInputForBlockMiningDataProvider.AddTransactionInputForBlockMining(blockHash,
                    new CrossChainTransactionInput
                    {
                        PreviousBlockHeight = blockHeight,
                        MethodName =
                            nameof(CrossChainContractImplContainer.CrossChainContractImplStub
                                .ReleaseCrossChainIndexingProposal),
                        Value = new ReleaseCrossChainIndexingProposalInput {ChainIdList = {toBeReleasedChainIdList}}
                            .ToByteString()
                    });
                var toBeReleasedCrossChainBlockData = AggregateToBeReleasedCrossChainData(
                    toBeReleasedChainIdList.Select(chainId =>
                        indexingProposalStatusList.ChainIndexingProposalStatus[chainId].ProposedCrossChainBlockData));
                return toBeReleasedCrossChainBlockData.ExtractCrossChainExtraDataFromCrossChainBlockData();
            }
                
            var pendingChainIdList = FindPendingStatusChainIdList(indexingProposalStatusList, utcNow);

            var crossChainBlockData =
                await GetCrossChainBlockDataForNextMining(blockHash, blockHeight, new HashSet<int>(pendingChainIdList));
            
            if (!crossChainBlockData.IsNullOrEmpty())
                _transactionInputForBlockMiningDataProvider.AddTransactionInputForBlockMining(blockHash,
                    new CrossChainTransactionInput
                    {
                        PreviousBlockHeight = blockHeight,
                        MethodName =
                            nameof(CrossChainContractImplContainer.CrossChainContractImplStub.ProposeCrossChainIndexing),
                        Value = crossChainBlockData.ToByteString()
                    });
            return ByteString.Empty;
        }

        public void UpdateCrossChainDataWithLib(Hash blockHash, long blockHeight)
        {
            // clear useless cache
            _transactionInputForBlockMiningDataProvider.ClearExpiredTransactionInput(blockHeight);
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        {
            var libDto = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
            return await _contractReaderFactory
                .Create(new ContractReaderContext
                {
                    BlockHash = libDto.BlockHash,
                    BlockHeight = libDto.BlockHeight,
                    ContractAddress = await GetCrossChainContractAddressAsync(new ChainContext
                    {
                        BlockHash = libDto.BlockHash,
                        BlockHeight = libDto.BlockHeight
                    })
                }).GetChainInitializationData.CallAsync(new Int32Value
                {
                    Value = chainId
                });
        }

        public async Task<Block> GetNonIndexedBlockAsync(long height)
        {
            return await _irreversibleBlockStateProvider.GetNotIndexedIrreversibleBlockByHeightAsync(height);
        }

        public async Task<ChainIdAndHeightDict> GetAllChainIdHeightPairsAtLibAsync()
        {
            var isReadyToCreateChainCache =
                await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            if (!isReadyToCreateChainCache)
                return new ChainIdAndHeightDict();
            var lib = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
            return await _contractReaderFactory
                .Create(new ContractReaderContext
                {
                    BlockHash = lib.BlockHash,
                    BlockHeight = lib.BlockHeight,
                    ContractAddress = await GetCrossChainContractAddressAsync(new ChainContext
                    {
                        BlockHash = lib.BlockHash,
                        BlockHeight = lib.BlockHeight
                    })
                }).GetAllChainsIdAndHeight
                .CallAsync(new Empty());
        }

        private async Task<CrossChainBlockData> GetCrossChainBlockDataForNextMining(Hash blockHash,
            long blockHeight, HashSet<int> excludeChainIdList)
        {
            var sideChainBlockData = await GetNonIndexedSideChainBlockDataAsync(blockHash, blockHeight, excludeChainIdList);
            var parentChainBlockData = await GetNonIndexedParentChainBlockDataAsync(blockHash, blockHeight, excludeChainIdList);

            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockDataList = {parentChainBlockData},
                SideChainBlockDataList = {sideChainBlockData}
            };
            return crossChainBlockData;
        }

        private async Task<GetIndexingProposalStatusOutput> GetIndexingProposalStatusAsync(
            Hash blockHash, long blockHeight, Timestamp timestamp)
        {
            var pendingProposal = await _contractReaderFactory
                .Create(new ContractReaderContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight,
                    ContractAddress = await GetCrossChainContractAddressAsync(new ChainContext
                    {
                        BlockHash = blockHash,
                        BlockHeight = blockHeight
                    }),
                    Timestamp = timestamp
                })
                .GetIndexingProposalStatus.CallAsync(new Empty());
            return pendingProposal;
        }

        private List<int> FindPendingStatusChainIdList(
            GetIndexingProposalStatusOutput pendingChainIndexingProposalStatusList, Timestamp timestamp)
        {
            return pendingChainIndexingProposalStatusList.ChainIndexingProposalStatus
                .Where(pair => !pair.Value.ToBeReleased && pair.Value.ExpiredTime.AddMilliseconds(500) > timestamp)
                .Select(pair => pair.Key).ToList();
        }
        
        private List<int> FindToBeReleasedChainIdList(
            GetIndexingProposalStatusOutput pendingChainIndexingProposalStatusList, Timestamp timestamp)
        {
            return pendingChainIndexingProposalStatusList.ChainIndexingProposalStatus
                .Where(pair => pair.Value.ToBeReleased && pair.Value.ExpiredTime > timestamp).Select(pair => pair.Key)
                .ToList();
        }

        private CrossChainBlockData AggregateToBeReleasedCrossChainData(IEnumerable<CrossChainBlockData> toBeIndexedList)
        {
            var res = new CrossChainBlockData();
            toBeIndexedList.Aggregate(res, (cur, element) =>
            {
                cur.ParentChainBlockDataList.Add(element.ParentChainBlockDataList);
                cur.SideChainBlockDataList.Add(element.SideChainBlockDataList);
                return cur;
            });

            return res;
        }
    }
}