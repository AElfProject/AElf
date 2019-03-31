using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Grpc
{
    public class CrossChainGrpcServerBase : CrossChainRpc.CrossChainRpcBase, ISingletonDependency
    {
        public ILogger<CrossChainGrpcServerBase> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }
        private readonly IBlockExtraDataExtractor _blockExtraDataExtractor;
        private readonly ICrossChainService _crossChainService;
        private readonly ILocalLibService _localLibService;
        private readonly CrossChainConfigOption _crossChainConfigOption;
        
        public CrossChainGrpcServerBase(CrossChainService crossChainService, IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOption,
            IBlockExtraDataExtractor blockExtraDataExtractor, ILocalLibService localLibService)
        {
            _crossChainService = crossChainService;
            _blockExtraDataExtractor = blockExtraDataExtractor;
            _localLibService = localLibService;
            LocalEventBus = NullLocalEventBus.Instance;
            _crossChainConfigOption = crossChainConfigOption.Value;
        }

        public override async Task RequestIndexingParentChain(RequestCrossChainBlockData request, 
            IServerStreamWriter<ResponseParentChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Parent Chain Server received IndexedInfo message.");
            await WriteResponseStream(request, responseStream, false);
        }
        
        public override async Task RequestIndexingSideChain(RequestCrossChainBlockData request, 
            IServerStreamWriter<ResponseSideChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Side Chain Server received IndexedInfo message.");
            await WriteResponseStream(request, responseStream, true);
        }

        public override Task<IndexingHandShakeReply> CrossChainIndexingShake(IndexingHandShake request, ServerCallContext context)
        {
            var splitRes = context.Peer.Split(':');
            LocalEventBus.PublishAsync(new GrpcServeNewChainReceivedEvent
            {
                CrossChainCommunicationContextDto = new GrpcCrossChainCommunicationContext
                {
                    TargetIp = splitRes[1],
                    TargetPort = request.ListeningPort,
                    RemoteChainId = request.ChainId,
                    RemoteIsSideChain = true,
                    CertificateFileName = ChainHelpers.ConvertChainIdToBase58(request.ChainId)
                }
            });
            //Logger.LogWarning($"Hand shake from chain {request.ChainId}, ip {splitRes[1]}, port {request.ListeningPort}");
            return Task.FromResult(new IndexingHandShakeReply{Result = true});
        }

        private async Task<IList<SideChainBlockData>> GetIndexedSideChainBlockInfoResult(Block block)
        {
            var crossChainBlockData =
                await _crossChainService.GetCrossChainBlockDataIndexedInStateAsync(block.GetHash(), block.Height);
            //Logger.LogTrace($"Indexed side chain block size {crossChainBlockData.SideChainBlockData.Count}");
            return crossChainBlockData.SideChainBlockData;
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
        
        private async Task<IResponseIndexingMessage> CreateParentChainResponse(Block block, int remoteSideChainId)
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
            foreach (var symbol in _crossChainConfigOption.ExtraDataSymbols)
            {
                var extraData = _blockExtraDataExtractor.ExtractOtherExtraData(symbol, block.Header);
                if(extraData != null)
                    responseParentChainBlockData.BlockData.ExtraData.Add(symbol, extraData);
            }
            
            var transactionStatusMerkleRoot =
                _blockExtraDataExtractor.ExtractTransactionStatusMerkleTreeRoot(block.Header);
            responseParentChainBlockData.BlockData.Root.TransactionStatusMerkleRoot = transactionStatusMerkleRoot;
            
            var crossChainExtra = _blockExtraDataExtractor.ExtractCrossChainExtraData(block.Header);
            if (crossChainExtra == null) 
                return responseParentChainBlockData;
            
            responseParentChainBlockData.BlockData.Root.CrossChainExtraData = crossChainExtra;
            var indexedSideChainBlockDataResult = await GetIndexedSideChainBlockInfoResult(block);
            var enumerableMerklePath = GetEnumerableMerklePath(indexedSideChainBlockDataResult, remoteSideChainId);
            foreach (var (sideChainHeight, merklePath) in enumerableMerklePath)
            {
                responseParentChainBlockData.BlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            }
            return responseParentChainBlockData;
        }

        private IResponseIndexingMessage CreateSideChainBlockData(Block block)
        {
            var transactionStatusMerkleRoot = _blockExtraDataExtractor.ExtractTransactionStatusMerkleTreeRoot(block.Header); 
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
            IServerStreamWriter<T> responseStream, bool requestSideChain) where T : IResponseIndexingMessage
        {
            var requestedHeight = request.NextHeight;
            var remoteChainId = request.FromChainId;
            while (true)
            {
                var block = await GetIrreversibleBlock(requestedHeight);
                if (block == null)
                    return;
                var res = requestSideChain
                    ? CreateSideChainBlockData(block)
                    : await CreateParentChainResponse(block, remoteChainId);
                await responseStream.WriteAsync((T) res);
                requestedHeight++;
            }
        }
            
        private async Task<Block> GetIrreversibleBlock(long height)
        {
            return await _localLibService.GetIrreversibleBlockByHeightAsync(height);
        }
    }
}