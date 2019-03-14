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

namespace AElf.CrossChain.Grpc.Server
{
    public class CrossChainGrpcServerBase : CrossChainRpc.CrossChainRpcBase, ISingletonDependency
    {
        public ILogger<CrossChainGrpcServerBase> Logger { get; set; }
        private ILocalEventBus LocalEventBus { get; }
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
            Logger = NullLogger<CrossChainGrpcServerBase>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
            _crossChainConfigOption = crossChainConfigOption.Value;
        }

        public override async Task RequestIndexingParentChain(RequestCrossChainBlockData request, IServerStreamWriter<ResponseParentChainBlockData> responseStream,
            ServerCallContext context)
        {
            Logger.LogDebug("Parent Chain Server received IndexedInfo message.");
            await WriteResponseStream(request, responseStream, false);
        }

        /// <summary>
        /// Response to recording request from side chain node.
        /// Many requests to many responses.
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task RequestParentChainDuplexStreaming(IAsyncStreamReader<RequestCrossChainBlockData> requestStream, 
            IServerStreamWriter<ResponseParentChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogDebug("Parent Chain Server received IndexedInfo message.");

//            while (await requestStream.MoveNext())
//            {
//                var requestInfo = requestStream.Current;
//                var requestedHeight = requestInfo.NextHeight;
//                var sideChainId = requestInfo.RemoteChainId;
//                
//                var block = await GetIrreversibleBlock(requestedHeight);
//                if (block == null)
//                {
//                    await responseStream.WriteAsync(new ResponseParentChainBlockData
//                    {
//                        Success = false
//                    });
//                    continue;
//                }
//
//                var res = await CreateParentChainResponse(block, sideChainId);
//                
//                await responseStream.WriteAsync(res);
//            }
        }
        
        /// <summary>
        /// Response to indexing request from main chain node.
        /// Many requests to many responses.
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task RequestSideChainDuplexStreaming(IAsyncStreamReader<RequestCrossChainBlockData> requestStream, 
            IServerStreamWriter<ResponseSideChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogDebug("Side Chain Server received IndexedInfo message.");

//            try
//            {
//                while (await requestStream.MoveNext())
//                {
//                    var requestInfo = requestStream.Current;
//                    var requestedHeight = requestInfo.NextHeight;
//                    
//                    var block = await GetIrreversibleBlock(
//                        requestedHeight);
//                    if (block == null)
//                    {
//                        await responseStream.WriteAsync(new ResponseSideChainBlockData
//                        {
//                            Success = false
//                        });
//                        continue;
//                    }
//                    
//                    var blockHeader = block.Header;
//                    var res = new ResponseSideChainBlockData
//                    {
//                        Success = blockHeader != null,
//                        BlockData = blockHeader == null ? null : new SideChainBlockData
//                        {
//                            SideChainHeight = requestedHeight,
//                            BlockHeaderHash = blockHeader.GetHash(),
//                            TransactionMKRoot = blockHeader.MerkleTreeRootOfTransactions,
//                            SideChainId = blockHeader.ChainId
//                        }
//                    };
//                    
//                    await responseStream.WriteAsync(res);
//                }
//            }
//            catch (Exception e)
//            {
//                Logger.LogError(e, "Side chain server out of service with exception.");
//            }
        }

        public override async Task RequestIndexingSideChain(RequestCrossChainBlockData request, IServerStreamWriter<ResponseSideChainBlockData> responseStream,
            ServerCallContext context)
        {
            Logger.LogDebug("Side Chain Server received IndexedInfo message.");
            await WriteResponseStream(request, responseStream, true);
        }

//        /// <summary>
//        /// Rpc interface for new chain connection.
//        /// </summary>
//        /// <param name="request"></param>
//        /// <param name="context"></param>
//        /// <returns></returns>
//        public override Task<IndexingRequestResult> RequestIndexing(IndexingRequestMessage request, ServerCallContext context)
//        {
//            var splitRes = context.Peer.Split(':');
//            LocalEventBus.PublishAsync(new GrpcServeNewChainReceivedEvent
//            {
//                CrossChainCommunicationContextDto = new GrpcCrossChainCommunicationContext
//                {
//                    TargetIp = splitRes[1],
//                    TargetPort = request.ListeningPort,
//                    RemoteChainId = request.SideChainId,
//                    RemoteIsSideChain = true,
//                    CertificateFileName = request.CertificateFileName
//                }
//            });
//            return Task.FromResult(new IndexingRequestResult{Result = true});
//        }


        private async Task<IList<SideChainBlockData>> GetIndexedSideChainBlockInfoResult(Block block)
        {
            var crossChainBlockData =
                await _crossChainService.GetIndexedCrossChainBlockDataAsync(block.GetHash(), block.Height);
            return crossChainBlockData.SideChainBlockData;
        }

        private IEnumerable<(long, MerklePath)> GetEnumerableMerklePath(IList<SideChainBlockData> indexedSideChainBlockDataResult, 
            int sideChainId)
        {
            var binaryMerkleTree = new BinaryMerkleTree();
            foreach (var blockInfo in indexedSideChainBlockDataResult)
            {
                binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
            }

            binaryMerkleTree.ComputeRootHash();
            // This is to tell side chain the merkle path for one side chain block,
            // which could be removed with subsequent improvement.
            // This assumes indexing multi blocks from one chain at once, actually only one block every time right now.
            for (var i = 0; i < indexedSideChainBlockDataResult.Count; i++)
            {
                var info = indexedSideChainBlockDataResult[i];
                if (!info.ChainId.Equals(sideChainId))
                    continue;
                var merklePath = binaryMerkleTree.GenerateMerklePath(i);
                yield return (info.Height, merklePath);
            }
        }
        
        private ByteString ExtractBlockExtraData(BlockHeader header, string extraDataType)
        {
            return _blockExtraDataExtractor.ExtractExtraData(extraDataType, header);
        }

        private async Task<IResponseIndexingMessage> CreateParentChainResponse(Block block, int parentChain)
        {
            var res = new ResponseParentChainBlockData
            {
                Success = true,
                BlockData = new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainHeight = block.Height,
                        SideChainTransactionsRoot =
                            Hash.LoadByteArray(block.Header.BlockExtraDatas[0].ToByteArray()),
                        ParentChainId = block.Header.ChainId
                    }
                }
            };
            var indexedSideChainBlockDataResult = await GetIndexedSideChainBlockInfoResult(block);

            var enumerableMerklePath = GetEnumerableMerklePath(indexedSideChainBlockDataResult, parentChain);
            foreach (var (sideChainHeight, merklePath) in enumerableMerklePath)
            {
                res.BlockData.IndexedMerklePath.Add(sideChainHeight, merklePath);
            }

            foreach (var symbol in _crossChainConfigOption.ExtraDataSymbols)
            {
                res.BlockData.ExtraData.Add(symbol, _blockExtraDataExtractor.ExtractExtraData(symbol, block.Header));
            }

            return res;
        }

        private IResponseIndexingMessage CreateSideChainBlockData(Block block, int sideChainId)
        {
            return new ResponseSideChainBlockData
            {
                Success = block.Header != null,
                BlockData = new SideChainBlockData
                {
                    SideChainHeight = block.Height,
                    BlockHeaderHash = block.GetHash(),
                    TransactionMKRoot = block.Header.MerkleTreeRootOfTransactions,
                    SideChainId = sideChainId
                }
            };
        }

        private async Task WriteResponseStream<T>(RequestCrossChainBlockData request, 
            IServerStreamWriter<T> responseStream, bool requestSideChain) where T : IResponseIndexingMessage
        {
            var requestedHeight = request.NextHeight;
            var remoteChainId = request.RemoteChainId;
            while (true)
            {
                var block = await GetIrreversibleBlock(requestedHeight);
                if (block == null)
                    return;
                var res = requestSideChain
                    ? CreateSideChainBlockData(block, remoteChainId)
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