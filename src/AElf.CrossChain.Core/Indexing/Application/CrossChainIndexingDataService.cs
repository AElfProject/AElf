using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Indexing.Application
{
    internal class CrossChainIndexingDataService : ICrossChainIndexingDataService
    {
        private readonly IReaderFactory _readerFactory;
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;
        private readonly ITransactionInputForBlockMiningDataProvider _transactionInputForBlockMiningDataProvider;
        private readonly IIrreversibleBlockStateProvider _irreversibleBlockStateProvider;
        private readonly TransactionPackingOptions _transactionPackingOptions;

        public ILogger<CrossChainIndexingDataService> Logger { get; set; }

        public CrossChainIndexingDataService(IReaderFactory readerFactory,
            IBlockCacheEntityConsumer blockCacheEntityConsumer,
            ITransactionInputForBlockMiningDataProvider transactionInputForBlockMiningDataProvider,
            IIrreversibleBlockStateProvider irreversibleBlockStateProvider,
            IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions)
        {
            _readerFactory = readerFactory;
            _blockCacheEntityConsumer = blockCacheEntityConsumer;
            _transactionInputForBlockMiningDataProvider = transactionInputForBlockMiningDataProvider;
            _irreversibleBlockStateProvider = irreversibleBlockStateProvider;
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;
        }

        private async Task<List<SideChainBlockData>> GetNonIndexedSideChainBlockDataAsync(Hash blockHash,
            long blockHeight)
        {
            var sideChainBlockDataList = new List<SideChainBlockData>();
            var sideChainIndexingInformationList = await _readerFactory.Create(blockHash, blockHeight)
                .GetSideChainIndexingInformationList.CallAsync(new Empty());
            foreach (var sideChainIndexingInformation in sideChainIndexingInformationList.IndexingInformationList)
            {
                var libDto = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
                var sideChainId = sideChainIndexingInformation.ChainId;
                var sideChainHeightInLibValue = await _readerFactory.Create(libDto.BlockHash, libDto.BlockHeight)
                    .GetSideChainHeight.CallAsync(new SInt32Value {Value = sideChainId});

                long toBeIndexedCount;
                long targetHeight;
                var sideChainHeightInLib = sideChainHeightInLibValue?.Value ?? 0;
                if (sideChainHeightInLib > 0)
                {
                    targetHeight = sideChainIndexingInformation.IndexedHeight + 1;
                    toBeIndexedCount = CrossChainConstants.DefaultBlockCacheEntityCount;
                    Logger.LogTrace(
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
                    Logger.LogTrace(
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
                    Logger.LogTrace(
                        $"Got height [{sideChainBlockDataFromCache.First().Height} - {sideChainBlockDataFromCache.Last().Height} ]" +
                        $" from side chain {ChainHelper.ConvertChainIdToBase58(sideChainIndexingInformation.ChainId)}.");
                    sideChainBlockDataList.AddRange(sideChainBlockDataFromCache);
                }
            }

            return sideChainBlockDataList;
        }

        private async Task<List<ParentChainBlockData>> GetNonIndexedParentChainBlockDataAsync(Hash blockHash,
            long blockHeight)
        {
            var parentChainBlockDataList = new List<ParentChainBlockData>();
            var libExists = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            if (!libExists)
                return parentChainBlockDataList;

            var returnValue = await _readerFactory.Create(blockHash, blockHeight).GetParentChainId
                .CallAsync(new Empty());
            var parentChainId = returnValue?.Value ?? 0;
            if (parentChainId == 0)
            {
                //Logger.LogTrace("No configured parent chain");
                // no configured parent chain
                return parentChainBlockDataList;
            }

            int length = CrossChainConstants.DefaultBlockCacheEntityCount;
            var heightInState = (await _readerFactory.Create(blockHash, blockHeight).GetParentChainHeight
                .CallAsync(new Empty())).Value;

            var targetHeight = heightInState + 1;
            Logger.LogTrace($"Target height {targetHeight}");

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
                Logger.LogTrace(
                    $"Got height [{parentChainBlockDataList.First().Height} - {parentChainBlockDataList.Last().Height} ]" +
                    $" from parent chain {ChainHelper.ConvertChainIdToBase58(parentChainId)}.");
            return parentChainBlockDataList;
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var crossChainBlockData = await _readerFactory.Create(blockHash, blockHeight)
                .GetIndexedCrossChainBlockDataByHeight.CallAsync(new SInt64Value {Value = blockHeight});
            return crossChainBlockData;
        }

        public async Task<IndexedSideChainBlockData> GetIndexedSideChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var indexedSideChainBlockData = await _readerFactory.Create(blockHash, blockHeight)
                .GetIndexedSideChainBlockDataByHeight.CallAsync(new SInt64Value {Value = blockHeight});
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
            var pendingProposal = await GetPendingCrossChainIndexingProposalAsync(blockHash, blockHeight, timestamp);
            return pendingProposal != null && pendingProposal.ToBeReleased && pendingProposal.ExpiredTime > timestamp;
        }

        public async Task<ByteString> PrepareExtraDataForNextMiningAsync(Hash blockHash, long blockHeight)
        {
            if (!_transactionPackingOptions.IsTransactionPackable)
                return ByteString.Empty;

            var utcNow = TimestampHelper.GetUtcNow();
            var pendingProposal = await GetPendingCrossChainIndexingProposalAsync(blockHash, blockHeight, utcNow);

            if (pendingProposal == null || pendingProposal.ExpiredTime.AddMilliseconds(500) <= utcNow)
            {
                // propose new cross chain indexing data if pending proposal is null or expired 
                var crossChainBlockData = await GetCrossChainBlockDataForNextMining(blockHash, blockHeight);
                if (!crossChainBlockData.IsNullOrEmpty())
                    _transactionInputForBlockMiningDataProvider.AddTransactionInputForBlockMining(blockHash,
                        new CrossChainTransactionInput
                        {
                            PreviousBlockHeight = blockHeight,
                            MethodName =
                                nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing),
                            Value = crossChainBlockData.ToByteString()
                        });
                return ByteString.Empty;
            }

            if (!pendingProposal.ToBeReleased)
                return ByteString.Empty; // do nothing if pending proposal is not ready to be released

            // release pending proposal and unable to propose anything if it is ready
            _transactionInputForBlockMiningDataProvider.AddTransactionInputForBlockMining(blockHash,
                new CrossChainTransactionInput
                {
                    PreviousBlockHeight = blockHeight,
                    MethodName =
                        nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing),
                    Value = pendingProposal.ProposalId.ToByteString()
                });
            return ExtractCrossChainExtraDataFromCrossChainBlockData(pendingProposal.ProposedCrossChainBlockData);
        }


        public ByteString ExtractCrossChainExtraDataFromCrossChainBlockData(CrossChainBlockData crossChainBlockData)
        {
            if (crossChainBlockData.IsNullOrEmpty() || crossChainBlockData.SideChainBlockDataList.Count == 0)
                return ByteString.Empty;

            var txRootHashList = crossChainBlockData.SideChainBlockDataList
                .Select(scb => scb.TransactionStatusMerkleTreeRoot).ToList();

            var calculatedSideChainTransactionsRoot = BinaryMerkleTree.FromLeafNodes(txRootHashList).Root;
            Logger.LogInformation("Cross chain extra data generated.");
            return new CrossChainExtraData
                {
                    TransactionStatusMerkleTreeRoot = calculatedSideChainTransactionsRoot
                }
                .ToByteString();
        }

        public void UpdateCrossChainDataWithLib(Hash blockHash, long blockHeight)
        {
            // clear useless cache
            _transactionInputForBlockMiningDataProvider.ClearExpiredTransactionInput(blockHeight);
        }

        private async Task<CrossChainBlockData> GetCrossChainBlockDataForNextMining(Hash blockHash,
            long blockHeight)
        {
            var sideChainBlockData = await GetNonIndexedSideChainBlockDataAsync(blockHash, blockHeight);
            var parentChainBlockData = await GetNonIndexedParentChainBlockDataAsync(blockHash, blockHeight);

            var crossChainBlockData = new CrossChainBlockData
            {
                PreviousBlockHeight = blockHeight,
                ParentChainBlockDataList = {parentChainBlockData},
                SideChainBlockDataList = {sideChainBlockData}
            };
            return crossChainBlockData;
        }

        private async Task<GetPendingCrossChainIndexingProposalOutput> GetPendingCrossChainIndexingProposalAsync(
            Hash blockHash, long blockHeight, Timestamp timestamp)
        {
            var pendingProposal = await _readerFactory.Create(blockHash, blockHeight, timestamp)
                .GetPendingCrossChainIndexingProposal.CallAsync(new Empty());
            return pendingProposal;
        }
    }
}