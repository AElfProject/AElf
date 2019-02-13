using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.CrossChain;
using AElf.ChainController.EventMessages;
using AElf.Kernel;
using Grpc.Core;
using AElf.Common;
using AElf.Kernel.Managers;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Miner.Rpc.Server
{
    
    public class ParentChainBlockInfoRpcServer : ParentChainBlockInfoRpc.ParentChainBlockInfoRpcBase, ITransientDependency
    {
        private readonly IChainService _chainService;
        public ILogger<ParentChainBlockInfoRpcServer> Logger {get;set;}
        private IBlockChain BlockChain { get; set; }
        private readonly ICrossChainInfoReader _crossChainInfoReader;
        private ulong LibHeight { get; set; }

        private int _chainId;
        
        public ParentChainBlockInfoRpcServer(IChainService chainService, ICrossChainInfoReader crossChainInfoReader)
        {
            _chainService = chainService;
            Logger = NullLogger<ParentChainBlockInfoRpcServer>.Instance;
            _crossChainInfoReader = crossChainInfoReader;
        }

        public void Init(int chainId)
        {
            _chainId = chainId;
            BlockChain = _chainService.GetBlockChain(_chainId);
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
        public override async Task RecordDuplexStreaming(IAsyncStreamReader<RequestBlockInfo> requestStream, 
            IServerStreamWriter<ResponseParentChainBlockInfo> responseStream, ServerCallContext context)
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
                        await responseStream.WriteAsync(new ResponseParentChainBlockInfo
                        {
                            Success = false
                        });
                        continue;
                    }
                    IBlock block = await BlockChain.GetBlockByHeightAsync(requestedHeight);
                    
                    var res = new ResponseParentChainBlockInfo
                    {
                        Success = block != null
                    };

                    if (block != null)
                    {
                        BlockHeader header = block.Header;
                        res.BlockInfo = new ParentChainBlockInfo
                        {
                            Root = new ParentChainBlockRootInfo
                            {
                                Height = requestedHeight,
                                SideChainTransactionsRoot = header.SideChainTransactionsRoot,
                                ChainId = header.ChainId
                            }
                        };
                        var indexedSideChainBlockInfoResult =
                            await _crossChainInfoReader.GetIndexedSideChainBlockInfoResult(_chainId, requestedHeight);
                        if (indexedSideChainBlockInfoResult != null)
                        {
                            var binaryMerkleTree = new BinaryMerkleTree();
                            foreach (var blockInfo in indexedSideChainBlockInfoResult.SideChainBlockInfos)
                            {
                                binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
                            }

                            binaryMerkleTree.ComputeRootHash();
                            // This is to tell side chain the merkle path for one side chain block, which could be removed with subsequent improvement.
                            // This assumes indexing multi blocks from one chain at once, actually only one every time right now.
                            for (int i = 0; i < indexedSideChainBlockInfoResult.SideChainBlockInfos.Count; i++)
                            {
                                var info = indexedSideChainBlockInfoResult.SideChainBlockInfos[i];
                                if (!info.ChainId.Equals(sideChainId))
                                    continue;
                                var merklePath = binaryMerkleTree.GenerateMerklePath(i);
                                res.BlockInfo.IndexedBlockInfo.Add(info.Height, merklePath);
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

        /// <summary>
        /// Response to recording request from side chain node.
        /// One request to many responses. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /*public override async Task RecordServerStreaming(RequestBlockInfo request, IServerStreamWriter<ResponseParentChainBlockInfo> responseStream, ServerCallContext context)
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
                
                    var res = new ResponseParentChainBlockInfo
                    {
                        Success = block != null
                    };

                    if (res.Success)
                    {
                        res.BlockInfo = new ParentChainBlockInfo
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
                            .ToList().ForEach(kv => res.BlockInfo.IndexedBlockInfo.Add(kv.Key, kv.Value));
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