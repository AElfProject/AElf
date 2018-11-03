using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Grpc.Core;
using NLog;
using NServiceKit.Common.Extensions;
using AElf.Common;
using NLog.Fluent;

namespace AElf.Miner.Rpc.Server
{
    [LoggerName("ParentChainRpcServer")]
    public class ParentChainBlockInfoRpcServerImpl : ParentChainBlockInfoRpc.ParentChainBlockInfoRpcBase
    {
        private readonly IChainService _chainService;
        private readonly ILogger _logger;
        private IBlockChain BlockChain { get; set; }
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        public ParentChainBlockInfoRpcServerImpl(IChainService chainService, ILogger logger, 
            IBinaryMerkleTreeManager binaryMerkleTreeManager)
        {
            _chainService = chainService;
            _logger = logger;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
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
            _logger?.Debug("Parent Chain Server received IndexedInfo message.");

            try
            {
                while (await requestStream.MoveNext())
                {
                    var requestInfo = requestStream.Current;
                    var requestedHeight = requestInfo.NextHeight;
                    var sideChainId = requestInfo.ChainId;
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
                                SideChainBlockHeadersRoot = header?.SideChainBlockHeadersRoot,
                                SideChainTransactionsRoot = header?.SideChainTransactionsRoot,
                                ChainId = header?.ChainId
                            }
                        };
                        var tree = await _binaryMerkleTreeManager
                            .GetSideChainTransactionRootsMerkleTreeByHeightAsync(header?.ChainId, requestedHeight);
                        //Todo: this is to tell side chain the merkle path for one side chain block, which could be removed with subsequent improvement.
                        /*body?.IndexedInfo.Where(predicate: i => i.ChainId.Equals(sideChainId))
                            .Select((info, index) =>
                                new KeyValuePair<ulong, MerklePath>(info.Height, tree.GenerateMerklePath(index)))
                            .ForEach(kv => res.BlockInfo.IndexedBlockInfo.Add(kv.Key, kv.Value));*/
                        for (int i = 0; i < body?.IndexedInfo.Count; i++)
                        {
                            var info = body.IndexedInfo[i];
                            if (!info.ChainId.Equals(sideChainId))
                                continue;
                            var merklePath = tree.GenerateMerklePath(i);
                            if (merklePath == null)
                            {
                                _logger?.Trace($"tree.Root == null: {tree.Root == null}");
                                _logger?.Trace($"tree.LeafCount = {tree.LeafCount}, index = {i}");
                            }
                            res.BlockInfo.IndexedBlockInfo.Add(info.Height, tree.GenerateMerklePath(i));
                        }
                    }

                    //_logger?.Log(LogLevel.Debug, $"Parent Chain Server responsed IndexedInfo message of height {requestedHeight}");
                    await responseStream.WriteAsync(res);
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Miner server RecordDuplexStreaming failed.");
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
            _logger?.Trace("Parent Chain Server received IndexedInfo message.");

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
                                SideChainBlockHeadersRoot = header?.SideChainBlockHeadersRoot,
                                SideChainTransactionsRoot = header?.SideChainTransactionsRoot,
                                ChainId = header?.ChainId
                            }
                        };
                        
                        var tree = await _binaryMerkleTreeManager
                            .GetSideChainTransactionRootsMerkleTreeByHeightAsync(header?.ChainId, height);
                        //Todo: this is to tell side chain the height of side chain block in this main chain block, which could be removed with subsequent improvement.
                        body?.IndexedInfo.Where(predicate: i => i.ChainId.Equals(sideChainId))
                            .Select((info, index) =>
                                new KeyValuePair<ulong, MerklePath>(info.Height, tree.GenerateMerklePath(index)))
                            .ForEach(kv => res.BlockInfo.IndexedBlockInfo.Add(kv.Key, kv.Value));
                    }
                
                    //_logger?.Log(LogLevel.Trace, $"Parent Chain Server responsed IndexedInfo message of height {height}");
                    await responseStream.WriteAsync(res);

                    height++;
                }
            }
            catch(Exception e)
            {
                _logger?.Error(e, "Miner server RecordDuplexStreaming failed.");
            }
        }
    }
}