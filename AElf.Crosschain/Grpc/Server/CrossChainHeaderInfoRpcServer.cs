using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.CrossChain;
using AElf.ChainController.EventMessages;
using AElf.Crosschain.Grpc.Client;
using AElf.Kernel;
using AElf.Kernel.Services;
using Easy.MessageHub;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Crosschain.Grpc.Server
{
    public class CrossChainBlockDataRpcServer : CrossChainRpc.CrossChainRpcBase
    {
        public ILogger<CrossChainBlockDataRpcServer> Logger {get;set;}
        
        public ILocalEventBus LocalEventBus { get; set; }

        private readonly IBlockchainService _blockchainService;
        private readonly GrpcConfigOption _grpcConfigOption;
        private global::Grpc.Core.Server _grpcServer;

        public CrossChainBlockDataRpcServer(IBlockchainService blockchainService, GrpcConfigOption grpcConfigOption)
        {
            _blockchainService = blockchainService;
            _grpcConfigOption = grpcConfigOption;
            Logger = NullLogger<CrossChainBlockDataRpcServer>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public void Start(int chainId, string dir = "")
        {
            _grpcServer = CrossChainGrpcServerHelper.CreateServer(this, _grpcConfigOption, chainId, dir);
            _grpcServer.Start();
        }
        
        public void Close()
        {
            if (_grpcServer == null)
                return;
            _grpcServer.ShutdownAsync();
            _grpcServer = null;
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

            try
            {
                while (await requestStream.MoveNext())
                {
                    var requestInfo = requestStream.Current;
                    var requestedHeight = requestInfo.NextHeight;
                    var sideChainId = requestInfo.ChainId;
                    var block = await _blockchainService.GetIrreversibleBlockByHeightAsync(requestInfo.ChainId, requestedHeight);
                    if (block == null)
                    {
                        await responseStream.WriteAsync(new ResponseParentChainBlockData
                        {
                            Success = false
                        });
                        continue;
                    }

                    var res = new ResponseParentChainBlockData
                    {
                        Success = true,
                        BlockData = new ParentChainBlockData
                        {
                            Root = new ParentChainBlockRootInfo
                            {
                                Height = requestedHeight,
                                SideChainTransactionsRoot =
                                    block.Header.BlockExtraData.SideChainTransactionsRoot,
                                ChainId = block.Header.ChainId
                            }
                        }
                    };

                    var indexedSideChainBlockDataResult = GetIndexedSideChainBlockInfoResult(block);
                    
                    if (indexedSideChainBlockDataResult.Count != 0)
                    {
                        var binaryMerkleTree = new BinaryMerkleTree();
                        foreach (var blockInfo in indexedSideChainBlockDataResult)
                        {
                            binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
                        }

                        binaryMerkleTree.ComputeRootHash();
                        // This is to tell side chain the merkle path for one side chain block,
                        // which could be removed with subsequent improvement.
                        // This assumes indexing multi blocks from one chain at once, actually only one every time right now.
                        for (int i = 0; i < indexedSideChainBlockDataResult.Count; i++)
                        {
                            var info = indexedSideChainBlockDataResult[i];
                            if (!info.ChainId.Equals(sideChainId))
                                continue;
                            var merklePath = binaryMerkleTree.GenerateMerklePath(i);
                            res.BlockData.IndexedMerklePath.Add(info.Height, merklePath);
                        }
                    }
                    await responseStream.WriteAsync(res);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Miner server RecordDuplexStreaming failed.");
            }
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
            // TODO: verify the from address and the chain 
            Logger.LogDebug("Side Chain Server received IndexedInfo message.");

            try
            {
                while (await requestStream.MoveNext())
                {
                    var requestInfo = requestStream.Current;
                    var requestedHeight = requestInfo.NextHeight;
                    
                    
                    // Todo: Wait until 10 rounds for most peers to be ready.
                    var block = await _blockchainService.GetIrreversibleBlockByHeightAsync(requestInfo.ChainId,
                        requestedHeight);
                    if (block == null)
                    {
                        await responseStream.WriteAsync(new ResponseSideChainBlockData
                        {
                            Success = false
                        });
                        continue;
                    }
                    
                    var blockHeader = block.Header;
                    var res = new ResponseSideChainBlockData
                    {
                        Success = blockHeader != null,
                        BlockData = blockHeader == null ? null : new SideChainBlockData
                        {
                            Height = requestedHeight,
                            BlockHeaderHash = blockHeader.GetHash(),
                            TransactionMKRoot = blockHeader.MerkleTreeRootOfTransactions,
                            ChainId = blockHeader.ChainId
                        }
                    };
                    
                    await responseStream.WriteAsync(res);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Side chain server out of service with exception.");
            }
        }

        private IList<SideChainBlockData> GetIndexedSideChainBlockInfoResult(Block block)
        {
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(block.Body.TransactionList.Last().Params);
            return crossChainBlockData.SideChainBlockData;
        }

        public override Task<IndexingRequestResult> RequestIndexing(IndexingRequestMessage request, ServerCallContext context)
        {
            // todo: publish event for indexing new side chain
            LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = new GrpcClientBase
                {
                    BlockInfoCache = new BlockInfoCache(request.ChainId),
                    TargetIp = request.Ip,
                    TargetPort = request.Port
                }
            });
            return Task.FromResult(new IndexingRequestResult{Result = true});
        }

        /// <summary>
        /// Response to recording request from side chain node.
        /// One request to many responses. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /*public override async Task RecordServerStreaming(RequestCrossChainBlockData request, IServerStreamWriter<ResponseParentChainBlockData> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Parent Chain Server received IndexedInfo message.");

            try
            {
                var height = request.NextHeight;
                var sideChainId = request.ChainId;
                while (height <= await BlockChain.GetCurrentBlockHeightAsync())
                {
                    IBlock block = await BlockChain.GetBlockByHeightAsync(height);
                    BlockHeader header = block?.Header;
                    BlockBody body = block?.Body;
                
                    var res = new ResponseParentChainBlockData
                    {
                        Success = block != null
                    };

                    if (res.Success)
                    {
                        res.BlockData = new ParentChainBlockData
                        {
                            Root = new ParentChainBlockRootInfo
                            {
                                Height = height,
                                SideChainTransactionsRoot = header?.SideChainTransactionsRoot,
                                ChainId = header?.ChainId
                            }
                        };
                        
                        var tree = await _crossChainInfoReader.GetMerkleTreeForSideChainTransactionRootAsync(height);
                        //Todo: this is to tell side chain the height of side chain block in this main chain block, which could be removed with subsequent improvement.
                        body?.IndexedInfo.Where(predicate: i => i.ChainId.Equals(sideChainId))
                            .Select((info, index) =>
                                new KeyValuePair<ulong, MerklePath>(info.Height, tree.GenerateMerklePath(index)))
                            .ToList().ForEach(kv => res.BlockData.IndexedMerklePath.Add(kv.Key, kv.Value));
                    }
                
                    //Logger.LogLog(LogLevel.Trace, $"Parent Chain Server responsed IndexedInfo message of height {height}");
                    await responseStream.WriteAsync(res);

                    height++;
                }
            }
            catch(Exception e)
            {
                Logger.LogError(e, "Miner server RecordDuplexStreaming failed.");
            }
        }*/
    }
}