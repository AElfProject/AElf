using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.CrossChain;
using AElf.ChainController.EventMessages;
using AElf.Common.Attributes;
using AElf.Kernel;
using Grpc.Core;
using NLog;
using AElf.Common;
using AElf.Kernel.Managers;
using Easy.MessageHub;
using NLog.Fluent;

namespace AElf.Miner.Rpc.Server
{
    [LoggerName("ParentChainRpcServer")]
    public class ParentChainBlockInfoRpcServer : ParentChainBlockInfoRpc.ParentChainBlockInfoRpcBase
    {
        private readonly IChainService _chainService;
        private readonly ILogger _logger;
        private IBlockChain BlockChain { get; set; }
        private readonly ICrossChainInfoReader _crossChainInfoReader;
        private ulong LibHeight { get; set; }
        public ParentChainBlockInfoRpcServer(IChainService chainService, ILogger logger, ICrossChainInfoReader crossChainInfoReader)
        {
            _chainService = chainService;
            _logger = logger;
            _crossChainInfoReader = crossChainInfoReader;
        }

        public void Init(Hash chainId)
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
                    if (requestedHeight > LibHeight)
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
                        var indexedSideChainBlockInfoResult = await _crossChainInfoReader.GetIndexedSideChainBlockInfoResult(requestedHeight);
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
                                if (merklePath == null)
                                {
                                    // todo: this should not happen, only for debug
                                    _logger?.Debug($"tree.Root == null: {binaryMerkleTree.Root == null}");
                                    _logger?.Debug($"tree.LeafCount = {binaryMerkleTree.LeafCount}, index = {i}");
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
        /*public override async Task RecordServerStreaming(RequestBlockInfo request, IServerStreamWriter<ResponseParentChainBlockInfo> responseStream, ServerCallContext context)
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
                
                    //_logger?.Log(LogLevel.Trace, $"Parent Chain Server responsed IndexedInfo message of height {height}");
                    await responseStream.WriteAsync(res);

                    height++;
                }
            }
            catch(Exception e)
            {
                _logger?.Error(e, "Miner server RecordDuplexStreaming failed.");
            }
        }*/
    }
}