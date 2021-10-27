using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Application
{
    public class CrossChainResponseService : ICrossChainResponseService, ITransientDependency
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly IConsensusExtraDataProvider _consensusExtraDataProvider;

        public CrossChainResponseService(IBlockExtraDataService blockExtraDataService, 
            ICrossChainIndexingDataService crossChainIndexingDataService,
            IConsensusExtraDataProvider consensusExtraDataProvider)
        {
            _blockExtraDataService = blockExtraDataService;
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _consensusExtraDataProvider = consensusExtraDataProvider;
        }

        public async Task<SideChainBlockData> ResponseSideChainBlockDataAsync(long requestHeight)
        {
            var block = await _crossChainIndexingDataService.GetNonIndexedBlockAsync(requestHeight);
            if (block == null)
                return null;
            
            return new SideChainBlockData
            {
                Height = block.Height,
                BlockHeaderHash = block.GetHash(),
                TransactionStatusMerkleTreeRoot = block.Header.MerkleTreeRootOfTransactionStatus,
                ChainId = block.Header.ChainId
            };
        }

        public async Task<ParentChainBlockData> ResponseParentChainBlockDataAsync(long requestHeight, int remoteSideChainId)
        {
            var block = await _crossChainIndexingDataService.GetNonIndexedBlockAsync(requestHeight);
            if (block == null)
                return null;
            var parentChainBlockData = new ParentChainBlockData
            {
                Height = block.Height, ChainId = block.Header.ChainId
            };
            parentChainBlockData = FillExtraDataInResponse(parentChainBlockData, block.Header);

            if (parentChainBlockData.CrossChainExtraData == null)
            {
                return parentChainBlockData;
            }

            var indexedSideChainBlockDataResult = await GetIndexedSideChainBlockDataResultAsync(block);
            var enumerableMerklePath = GetEnumerableMerklePath(indexedSideChainBlockDataResult, remoteSideChainId);
            foreach (var kv in enumerableMerklePath)
            {
                parentChainBlockData.IndexedMerklePath.Add(kv.Key, kv.Value);
            }
            
            return parentChainBlockData;
        }

        public async Task<ChainInitializationData> ResponseChainInitializationDataFromParentChainAsync(int chainId)
        {
            var chainInitializationData = await _crossChainIndexingDataService.GetChainInitializationDataAsync(chainId);
            return chainInitializationData;
        }

        private ParentChainBlockData FillExtraDataInResponse(ParentChainBlockData parentChainBlockData,
            BlockHeader blockHeader)
        {
            parentChainBlockData.TransactionStatusMerkleTreeRoot = blockHeader.MerkleTreeRootOfTransactionStatus;

            var crossChainExtraByteString =
                GetExtraDataFromHeader(blockHeader, CrossChainConstants.CrossChainExtraDataKey);

            var crossChainExtra = crossChainExtraByteString.IsNullOrEmpty()
                ? null
                : CrossChainExtraData.Parser.ParseFrom(crossChainExtraByteString);
            parentChainBlockData.CrossChainExtraData = crossChainExtra;

            parentChainBlockData.ExtraData.Add(GetExtraDataForExchange(blockHeader,
                new[] {_consensusExtraDataProvider.BlockHeaderExtraDataKey}));
            return parentChainBlockData;
        }

        private async Task<List<SideChainBlockData>> GetIndexedSideChainBlockDataResultAsync(Block block)
        {
            var indexedSideChainBlockData =
                await _crossChainIndexingDataService.GetIndexedSideChainBlockDataAsync(block.GetHash(), block.Height);
            return indexedSideChainBlockData.SideChainBlockDataList.ToList();
        }
        
        private Dictionary<long, MerklePath> GetEnumerableMerklePath(IList<SideChainBlockData> indexedSideChainBlockDataResult, 
            int sideChainId)
        {
            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(
                indexedSideChainBlockDataResult.Select(sideChainBlockData =>
                    sideChainBlockData.TransactionStatusMerkleTreeRoot));
            
            // This is to tell side chain the merkle path for one side chain block,
            // which could be removed with subsequent improvement.
            var res = new Dictionary<long, MerklePath>();
            for (var i = 0; i < indexedSideChainBlockDataResult.Count; i++)
            {
                var info = indexedSideChainBlockDataResult[i];
                if (info.ChainId != sideChainId)
                    continue;

                var merklePath = binaryMerkleTree.GenerateMerklePath(i);
                res.Add(info.Height, merklePath);
            }
            
            return res;
        }
        
        private ByteString GetExtraDataFromHeader(BlockHeader header, string symbol)
        {
            return _blockExtraDataService.GetExtraDataFromBlockHeader(symbol, header);
        }
        
        private Dictionary<string, ByteString> GetExtraDataForExchange(BlockHeader header, IEnumerable<string> symbolsOfExchangedExtraData)
        {
            var res = new Dictionary<string, ByteString>();
            foreach (var symbol in symbolsOfExchangedExtraData)
            {
                var extraData = GetExtraDataFromHeader(header, symbol);
                if (extraData != null)
                    res.Add(symbol, extraData);
            }
            
            return res;
        }
    }
}