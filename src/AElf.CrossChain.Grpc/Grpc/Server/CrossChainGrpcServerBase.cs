using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Grpc
{
    public class CrossChainGrpcServerBase : CrossChainRpc.CrossChainRpcBase, ISingletonDependency
    {
        public ILogger<CrossChainGrpcServerBase> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }
        private readonly IBlockchainService _blockchainService;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        private readonly IBasicCrossChainDataProvider _crossChainDataProvider;
        private readonly ICrossChainExtraDataExtractor _crossChainExtraDataExtractor;
        
        public CrossChainGrpcServerBase(IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption,
            IBlockExtraDataService blockExtraDataService,
            IBasicCrossChainDataProvider crossChainDataProvider, IBlockchainService blockchainService)
        {
            // TODO: Cleanup BlockExtraDataExtractor
            _crossChainExtraDataExtractor = new CrossChainExtraDataExtractor(blockExtraDataService);
            _crossChainDataProvider = crossChainDataProvider;
            _blockchainService = blockchainService;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public override async Task RequestIndexingFromParentChain(RequestCrossChainBlockData request, 
            IServerStreamWriter<ResponseParentChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Parent Chain Server received IndexedInfo message.");
            await WriteResponseStream(request, responseStream, true);
            var splitRes = context.Peer.Split(':');
            PublishCrossChainRequestReceivedEvent(splitRes[1], request.ListeningPort, request.FromChainId);
        }
        
        public override async Task RequestIndexingFromSideChain(RequestCrossChainBlockData request, 
            IServerStreamWriter<ResponseSideChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Side Chain Server received IndexedInfo message.");
            await WriteResponseStream(request, responseStream, false);
            var splitRes = context.Peer.Split(':');
            PublishCrossChainRequestReceivedEvent(splitRes[1], request.ListeningPort, request.FromChainId);
        }

        public override Task<IndexingHandShakeReply> CrossChainIndexingShake(IndexingHandShake request, ServerCallContext context)
        {
            Logger.LogTrace($"Received shake from chain {ChainHelpers.ConvertChainIdToBase58(request.ChainId)}.");
            var splitRes = context.Peer.Split(':');
            PublishCrossChainRequestReceivedEvent(splitRes[1], request.ListeningPort, request.ChainId);
            return Task.FromResult(new IndexingHandShakeReply{Result = true});
        }

        public override async Task<ChainInitializationResponse> RequestChainInitializationContextFromParentChain(ChainInitializationRequest request, ServerCallContext context)
        {
            return new ChainInitializationResponse
            {
                SideChainInitializationContext = await GetChainInitializationContextAsync(request.ChainId)
            };
        }

        private async Task<IList<SideChainBlockData>> GetIndexedSideChainBlockInfoResult(Block block)
        {
            var message =
                await _crossChainDataProvider.GetIndexedCrossChainBlockDataAsync(block.GetHash(), block.Height);
            //Logger.LogTrace($"Indexed side chain block size {crossChainBlockData.SideChainBlockData.Count}");
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(message.ToByteString());
            return crossChainBlockData.SideChainBlockData
                .Select(m => SideChainBlockData.Parser.ParseFrom(m.ToByteString())).ToList();
        }

        private IEnumerable<(long, MerklePath)> GetEnumerableMerklePath(IList<SideChainBlockData> indexedSideChainBlockDataResult, 
            int sideChainId)
        {
            var binaryMerkleTree = new BinaryMerkleTree();
            foreach (var blockInfo in indexedSideChainBlockDataResult)
            {
                binaryMerkleTree.AddNode(blockInfo.TransactionMerkleTreeRoot);
            }

            binaryMerkleTree.ComputeRootHash();
            // This is to tell side chain the merkle path for one side chain block,
            // which could be removed with subsequent improvement.
            // This assumes indexing multi blocks from one chain at once, actually only one block every time right now.
            var merklepathList = new List<(long, MerklePath)>();
            for (var i = 0; i < indexedSideChainBlockDataResult.Count; i++)
            {
                var info = indexedSideChainBlockDataResult[i];
                if (!info.ChainId.Equals(sideChainId))
                    continue;
                var merklePath = binaryMerkleTree.GenerateMerklePath(i);
                merklepathList.Add((info.Height, merklePath));
            }
                //Logger.LogTrace($"Got merkle path list size {merklepathList.Count}");
            return merklepathList;
        }
        
        private async Task<IResponseIndexingMessage> CreateResponseForSideChain(Block block, int remoteSideChainId,
            long parentChainHeightOfCreation)
        {
            var responseParentChainBlockData = new ResponseParentChainBlockData
            {
                Success = true,
                BlockData = new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainHeight = block.Height,
                        ParentChainId = block.Header.ChainId
                    }
                }
            };
            responseParentChainBlockData = FillExtraDataInResponse(responseParentChainBlockData, block.Header,
                block.Height >= parentChainHeightOfCreation);

            if (responseParentChainBlockData.BlockData.Root.CrossChainExtraData == null) 
                return responseParentChainBlockData;
            
            var indexedSideChainBlockDataResult = await GetIndexedSideChainBlockInfoResult(block);
            var enumerableMerklePath = GetEnumerableMerklePath(indexedSideChainBlockDataResult, remoteSideChainId);
            foreach (var (sideChainHeight, merklePath) in enumerableMerklePath)
            {
                responseParentChainBlockData.BlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            }
            return responseParentChainBlockData;
        }

        private IResponseIndexingMessage CreateSideChainResponse(Block block)
        {
            var transactionStatusMerkleRoot = _crossChainExtraDataExtractor.ExtractTransactionStatusMerkleTreeRoot(block.Header); 
            return new ResponseSideChainBlockData
            {
                Success = block.Header != null,
                BlockData = new SideChainBlockData
                {
                    SideChainHeight = block.Height,
                    BlockHeaderHash = block.GetHash(),
                    TransactionMerkleTreeRoot = transactionStatusMerkleRoot,
                    SideChainId = block.Header.ChainId
                }
            };
        }

        private async Task WriteResponseStream<T>(RequestCrossChainBlockData request, 
            IServerStreamWriter<T> responseStream, bool isSideChainRequest) where T : IResponseIndexingMessage
        {
            var requestedHeight = request.NextHeight;
            var remoteChainId = request.FromChainId;
            var parentChainHeightOfCreation = isSideChainRequest ?
                (await GetChainInitializationContextAsync(remoteChainId)).ParentChainHeightOfCreation : 0;
            while (true)
            {
                var block = await GetIrreversibleBlock(requestedHeight);
                if (block == null)
                    return;
                var res = isSideChainRequest
                    ? await CreateResponseForSideChain(block, remoteChainId, parentChainHeightOfCreation)
                    : CreateSideChainResponse(block);
                    
                await responseStream.WriteAsync((T) res);
                requestedHeight++;
            }
        }
            
        private async Task<Block> GetIrreversibleBlock(long height)
        {
            return await _blockchainService.GetIrreversibleBlockByHeightAsync(height);
        }

        private async Task<ChainInitializationContext> GetChainInitializationContextAsync(int chainId)
        {
            var libDto = await _blockchainService.GetLibHashAndHeight();
            var message = await _crossChainDataProvider.GetChainInitializationContextAsync(chainId, libDto.BlockHash,
                libDto.BlockHeight);

            return message==null?null:ChainInitializationContext.Parser.ParseFrom(message.ToByteString());
        }

        private ResponseParentChainBlockData FillExtraDataInResponse(ResponseParentChainBlockData responseParentChainBlockData, 
            BlockHeader blockHeader, bool needOtherExtraData)
        {
            var transactionStatusMerkleRoot =
                _crossChainExtraDataExtractor.ExtractTransactionStatusMerkleTreeRoot(blockHeader);
            responseParentChainBlockData.BlockData.Root.TransactionStatusMerkleRoot = transactionStatusMerkleRoot;
            
            var crossChainExtra = _crossChainExtraDataExtractor.ExtractCrossChainData(blockHeader);
            responseParentChainBlockData.BlockData.Root.CrossChainExtraData = crossChainExtra;
            
            if(needOtherExtraData)
            {
                // only pack extra information after side chain creation
                // but the problem of communication data size still exists 
                responseParentChainBlockData.BlockData.ExtraData.Add(_crossChainExtraDataExtractor.ExtractCommonExtraDataForExchange(blockHeader));
            }

            return responseParentChainBlockData;
        }

        private void PublishCrossChainRequestReceivedEvent(string ip, int port, int chainId)
        {
            LocalEventBus.PublishAsync(new GrpcCrossChainRequestReceivedEvent
            {
                CrossChainCommunicationContextDto = new GrpcCrossChainCommunicationContext
                {
                    TargetIp = ip,
                    TargetPort = port,
                    RemoteChainId = chainId
                }
            });
        }
    }
}