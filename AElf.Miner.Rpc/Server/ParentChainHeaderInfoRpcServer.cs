using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.CrossChain;
using AElf.Common.Attributes;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Grpc.Core;
using AElf.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Miner.Rpc.Server
{
    [LoggerName("ParentChainRpcServer")]
    public class ParentChainBlockInfoRpcServer : ParentChainBlockInfoRpc.ParentChainBlockInfoRpcBase
    {
        private readonly IChainService _chainService;
        public ILogger<ParentChainBlockInfoRpcServer> Logger {get;set;}
        private IBlockChain BlockChain { get; set; }
        private readonly ICrossChainInfoReader _crossChainInfoReader;
        public ParentChainBlockInfoRpcServer(IChainService chainService, ICrossChainInfoReader crossChainInfoReader)
        {
            _chainService = chainService;
            Logger = NullLogger<ParentChainBlockInfoRpcServer>.Instance;
            _crossChainInfoReader = crossChainInfoReader;
        }

        public void Init(Hash chainId)
        {
            BlockChain = _chainService.GetBlockChain(chainId);
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
                    var currentHeight = await BlockChain.GetCurrentBlockHeightAsync();
                    if (currentHeight - requestedHeight < (ulong)GlobalConfig.InvertibleChainHeight)
                    {
                        await responseStream.WriteAsync(new ResponseParentChainBlockInfo
                        {
                            Success = false
                        });
                        continue;
                    }
                    IBlock block = await BlockChain.GetBlockByHeightAsync(requestedHeight);
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
                                Height = requestedHeight,
                                SideChainTransactionsRoot = header?.SideChainTransactionsRoot,
                                ChainId = header?.ChainId
                            }
                        };
                        var tree = _crossChainInfoReader.GetMerkleTreeForSideChainTransactionRoot(requestedHeight);
                        if (tree != null)
                        {
                            // This is to tell side chain the merkle path for one side chain block, which could be removed with subsequent improvement.
                            // This assumes indexing multi blocks from one chain at once, actually only one every time right now.
                            for (int i = 0; i < body?.IndexedInfo.Count; i++)
                            {
                                var info = body.IndexedInfo[i];
                                if (!info.ChainId.Equals(sideChainId))
                                    continue;
                                var merklePath = tree.GenerateMerklePath(i);
                                if (merklePath == null)
                                {
                                    Logger.LogTrace($"tree.Root == null: {tree.Root == null}");
                                    Logger.LogTrace($"tree.LeafCount = {tree.LeafCount}, index = {i}");
                                }
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
        public override async Task RecordServerStreaming(RequestBlockInfo request, IServerStreamWriter<ResponseParentChainBlockInfo> responseStream, ServerCallContext context)
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
                        
                        var tree = _crossChainInfoReader.GetMerkleTreeForSideChainTransactionRoot(height);
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
        }
    }
}