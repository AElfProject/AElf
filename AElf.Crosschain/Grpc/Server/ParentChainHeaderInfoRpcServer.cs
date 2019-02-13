using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.CrossChain;
using AElf.ChainController.EventMessages;
using AElf.Kernel;
using Easy.MessageHub;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Crosschain.Grpc.Server
{
    public class ParentChainBlockInfoRpcServer : ParentChainRpc.ParentChainRpcBase
    {
        private readonly IChainService _chainService;
        public ILogger<ParentChainBlockInfoRpcServer> Logger {get;set;}
        private IBlockChain BlockChain { get; set; }
        private ulong LibHeight { get; set; }
        public ParentChainBlockInfoRpcServer(IChainService chainService)
        {
            _chainService = chainService;
            Logger = NullLogger<ParentChainBlockInfoRpcServer>.Instance;
        }

        public void Init(int chainId)
        {
            BlockChain = _chainService.GetBlockChain(chainId);
            MessageHub.Instance.Subscribe<NewLibFound>(newFoundLib => { LibHeight = newFoundLib.Height; });
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
                    if (requestedHeight > LibHeight)
                    {
                        await responseStream.WriteAsync(new ResponseParentChainBlockData
                        {
                            Success = false
                        });
                        continue;
                    }
                    IBlock block = await BlockChain.GetBlockByHeightAsync(requestedHeight);
                    
                    var res = new ResponseParentChainBlockData
                    { 
                        Success = block != null
                    };

                    if (block != null)
                    {
                        BlockHeader header = block.Header;
                        res.BlockData = new ParentChainBlockData
                        {
                            Root = new ParentChainBlockRootInfo
                            {
                                Height = requestedHeight,
                                SideChainTransactionsRoot = header.BlockExtraData.SideChainTransactionsRoot,
                                ChainId = header.ChainId
                            }
                        };
                        IndexedSideChainBlockDataResult indexedSideChainBlockDataResult = 
                            await GetIndexedSideChainBlockInfoResult(requestedHeight);
                        
                        if (indexedSideChainBlockDataResult != null)
                        {
                            var binaryMerkleTree = new BinaryMerkleTree();
                            foreach (var blockInfo in indexedSideChainBlockDataResult.SideChainBlockData)
                            {
                                binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
                            }

                            binaryMerkleTree.ComputeRootHash();
                            // This is to tell side chain the merkle path for one side chain block,
                            // which could be removed with subsequent improvement.
                            // This assumes indexing multi blocks from one chain at once, actually only one every time right now.
                            for (int i = 0; i < indexedSideChainBlockDataResult.SideChainBlockData.Count; i++)
                            {
                                var info = indexedSideChainBlockDataResult.SideChainBlockData[i];
                                if (!info.ChainId.Equals(sideChainId))
                                    continue;
                                var merklePath = binaryMerkleTree.GenerateMerklePath(i);
                                res.BlockData.IndexedMerklePath.Add(info.Height, merklePath);
                            }
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

        private async Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResult(ulong requestedHeight)
        {
            // todo: extract side chain block info from blocks.
            throw new NotImplementedException();
        }

        public override Task<IndexingRequestResult> RequestIndexing(IndexingRequestMessage request, ServerCallContext context)
        {
            // todo: publish event for indexing new side chain
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