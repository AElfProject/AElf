using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc
{
    public interface IParentChainServerService
    {
        Task<CrossChainResponse> GenerateResponseAsync(Block block, int remoteSideChainId);

        Task<SideChainInitializationContext> GetChainInitializationContextAsync(int chainId, LastIrreversibleBlockDto libDto);
    }

    internal class ParentChainServerService : IParentChainServerService, ITransientDependency
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly ICrossChainDataProvider _crossChainDataProvider;

        public ParentChainServerService(IBlockExtraDataService blockExtraDataService, ICrossChainDataProvider crossChainDataProvider)
        {
            _blockExtraDataService = blockExtraDataService;
            _crossChainDataProvider = crossChainDataProvider;
        }

        public async Task<CrossChainResponse> GenerateResponseAsync(Block block, int remoteSideChainId)
        {
            var responseParentChainBlockData = new CrossChainResponse
            {
                BlockData = new BlockData
                {
                    ChainId = block.Header.ChainId,
                    Height = block.Height
                }
            };
            var parentChainBlockData = new ParentChainBlockData
            {
                ParentChainHeight = block.Height, ParentChainId = block.Header.ChainId
            };
            parentChainBlockData = FillExtraDataInResponse(parentChainBlockData, block.Header);

            if (parentChainBlockData.CrossChainExtraData == null)
            {
                responseParentChainBlockData.BlockData.Payload = parentChainBlockData.ToByteString();
                return responseParentChainBlockData;
            }

            var indexedSideChainBlockDataResult = await GetIndexedSideChainBlockDataResult(block);
            var enumerableMerklePath = GetEnumerableMerklePath(indexedSideChainBlockDataResult, remoteSideChainId);
            foreach (var (sideChainHeight, merklePath) in enumerableMerklePath)
            {
                parentChainBlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            }
            
            responseParentChainBlockData.BlockData.Payload = parentChainBlockData.ToByteString();
            return responseParentChainBlockData;
        }

        public async Task<SideChainInitializationContext> GetChainInitializationContextAsync(int chainId, LastIrreversibleBlockDto libDto)
        {
            var chainInitializationContext =
                await _crossChainDataProvider.GetChainInitializationContextAsync(chainId, libDto.BlockHash,
                    libDto.BlockHeight);
            var sideChainInitializationResponse = new SideChainInitializationContext
            {
                ChainId = chainInitializationContext.ChainId,
                Creator = chainInitializationContext.Creator,
                CreationTimestamp = chainInitializationContext.CreationTimestamp,
                ParentChainHeightOfCreation = chainInitializationContext.ParentChainHeightOfCreation
            };
            sideChainInitializationResponse.ExtraInformation.AddRange(chainInitializationContext.ExtraInformation);
            return sideChainInitializationResponse;
        }
        
        private ParentChainBlockData FillExtraDataInResponse(ParentChainBlockData parentChainBlockData, BlockHeader blockHeader)
        {
            var transactionStatusMerkleRoot = GetTransactionStatusMerkleTreeRootFromHeader(blockHeader);

            parentChainBlockData.TransactionStatusMerkleRoot = transactionStatusMerkleRoot;

            var crossChainExtraByteString = GetExtraDataFromHeader(blockHeader, "CrossChain");
            var crossChainExtra = crossChainExtraByteString == ByteString.Empty || crossChainExtraByteString == null
                ? null
                : CrossChainExtraData.Parser.ParseFrom(crossChainExtraByteString);
            parentChainBlockData.CrossChainExtraData = crossChainExtra;

            parentChainBlockData.ExtraData.Add(GetExtraDataForExchange(blockHeader,
                new[] {"Consensus"}));
            return parentChainBlockData;
        }
        
        private async Task<List<SideChainBlockData>> GetIndexedSideChainBlockDataResult(Block block)
        {
            var crossChainBlockData =
                await _crossChainDataProvider.GetIndexedCrossChainBlockDataAsync(block.GetHash(), block.Height);
            //Logger.LogTrace($"Indexed side chain block size {crossChainBlockData.SideChainBlockData.Count}");
            //var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(message.ToByteString());
            return crossChainBlockData.SideChainBlockData
                .Select(m => SideChainBlockData.Parser.ParseFrom(m.ToByteString())).ToList();
        }
        
        private IEnumerable<(long, MerklePath)> GetEnumerableMerklePath(IList<SideChainBlockData> indexedSideChainBlockDataResult, 
            int sideChainId)
        {
            var binaryMerkleTree = new BinaryMerkleTree();
            foreach (var sideChainBlockData in indexedSideChainBlockDataResult)
            {
                binaryMerkleTree.AddNode(sideChainBlockData.TransactionMerkleTreeRoot);
            }

            binaryMerkleTree.ComputeRootHash();
            // This is to tell side chain the merkle path for one side chain block,
            // which could be removed with subsequent improvement.
            var merklepathList = new List<(long, MerklePath)>();
            for (var i = 0; i < indexedSideChainBlockDataResult.Count; i++)
            {
                var info = indexedSideChainBlockDataResult[i];
                if (!info.SideChainId.Equals(sideChainId))
                    continue;
                var merklePath = binaryMerkleTree.GenerateMerklePath(i);
                merklepathList.Add((info.SideChainHeight, merklePath));
            }
            
            return merklepathList;
        }
        
        private Hash GetTransactionStatusMerkleTreeRootFromHeader(BlockHeader header)
        {
            return Hash.Parser.ParseFrom(_blockExtraDataService.GetMerkleTreeRootExtraDataForTransactionStatus(header));
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
                if(extraData != null)
                    res.Add(symbol, extraData);
            }
            
            return res;
        }
    }
}

