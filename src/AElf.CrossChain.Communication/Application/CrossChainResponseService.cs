using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Application
{
    public class CrossChainResponseService : ICrossChainResponseService, ITransientDependency
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly ICrossChainService _crossChainService;
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;

        public CrossChainResponseService(IBlockExtraDataService blockExtraDataService, 
            ICrossChainService crossChainService, ICrossChainIndexingDataService crossChainIndexingDataService)
        {
            _blockExtraDataService = blockExtraDataService;
            _crossChainService = crossChainService;
            _crossChainIndexingDataService = crossChainIndexingDataService;
        }

        public async Task<SideChainBlockData> ResponseSideChainBlockDataAsync(long requestHeight)
        {
            var block = await _crossChainService.GetNonIndexedBlockAsync(requestHeight);
            if (block == null)
                return null;
            
            return new SideChainBlockData
            {
                Height = block.Height,
                BlockHeaderHash = block.GetHash(),
                TransactionMerkleTreeRoot = block.Header.MerkleTreeRootOfTransactionStatus,
                ChainId = block.Header.ChainId
            };
        }

        public async Task<ParentChainBlockData> ResponseParentChainBlockDataAsync(long requestHeight, int remoteSideChainId)
        {
            var block = await _crossChainService.GetNonIndexedBlockAsync(requestHeight);
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
            var chainInitializationData = await _crossChainService.GetChainInitializationDataAsync(chainId);
            return chainInitializationData;
        }

        private ParentChainBlockData FillExtraDataInResponse(ParentChainBlockData parentChainBlockData, BlockHeader blockHeader)
        {
            parentChainBlockData.TransactionStatusMerkleRoot = blockHeader.MerkleTreeRootOfTransactionStatus;

            var crossChainExtraByteString = GetExtraDataFromHeader(blockHeader, "CrossChain");
            var crossChainExtra = crossChainExtraByteString == ByteString.Empty || crossChainExtraByteString == null
                ? null
                : CrossChainExtraData.Parser.ParseFrom(crossChainExtraByteString);
            parentChainBlockData.CrossChainExtraData = crossChainExtra;

            parentChainBlockData.ExtraData.Add(GetExtraDataForExchange(blockHeader,
                new[] {"Consensus"}));
            return parentChainBlockData;
        }
        
        private async Task<List<SideChainBlockData>> GetIndexedSideChainBlockDataResultAsync(Block block)
        {
            var indexedSideChainBlockData =
                await _crossChainIndexingDataService.GetIndexedSideChainBlockDataAsync(block.GetHash(), block.Height);
            return indexedSideChainBlockData.SideChainBlockData.ToList();
        }
        
        private Dictionary<long, MerklePath> GetEnumerableMerklePath(IList<SideChainBlockData> indexedSideChainBlockDataResult, 
            int sideChainId)
        {
            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(
                indexedSideChainBlockDataResult.Select(sideChainBlockData =>
                    sideChainBlockData.TransactionMerkleTreeRoot));
            
            // This is to tell side chain the merkle path for one side chain block,
            // which could be removed with subsequent improvement.
            var res = new Dictionary<long, MerklePath>();
            for (var i = 0; i < indexedSideChainBlockDataResult.Count; i++)
            {
                var info = indexedSideChainBlockDataResult[i];
                if (!info.ChainId.Equals(sideChainId))
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