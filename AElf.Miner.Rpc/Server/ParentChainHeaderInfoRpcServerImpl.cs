using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Kernel;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Server
{
    [LoggerName("ParentChainRpcServer")]
    public class ParentChainBlockInfoRpcServerImpl : ParentChainBlockInfoRpc.ParentChainBlockInfoRpcBase
    {
        private readonly IChainService _chainService;
        private readonly ILogger _logger;
        private IBlockChain BlockChain { get; set; }
        public ParentChainBlockInfoRpcServerImpl(IChainService chainService, ILogger logger)
        {
            _chainService = chainService;
            _logger = logger;
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
            _logger?.Log(LogLevel.Debug, "Parent Chain Server received IndexedInfo message.");

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
                            Height = requestedHeight,
                            SideChainBlockHeadersRoot = header?.SideChainBlockHeadersRoot,
                            SideChainTransactionsRoot = header?.SideChainTransactionsRoot,
                            ChainId = header?.ChainId
                        };
                        //Todo: this is to tell side chain the height of side chain block in this main chain block, which could be removed with subsequent improvement.
                        res.BlockInfo.IndexedBlockHeight.Add(body?.IndexedInfo.Aggregate(new List<ulong>(), (h, i) =>
                        {
                            if (i.ChainId.Equals(sideChainId))
                                h.Add(i.Height);
                            return h;
                        }));
                    }
                    
                    _logger?.Log(LogLevel.Debug, $"Parent Chain Server responsed IndexedInfo message of height {requestedHeight}");
                    await responseStream.WriteAsync(res);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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
            _logger?.Log(LogLevel.Debug, "Parent Chain Server received IndexedInfo message.");

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
                            Height = height,
                            SideChainBlockHeadersRoot = header?.SideChainBlockHeadersRoot,
                            SideChainTransactionsRoot = header?.SideChainTransactionsRoot,
                            ChainId = header?.ChainId
                        };
                        //Todo: this is to tell side chain the height of side chain block in this main chain block, which could be removed with subsequent improvement.
                        res.BlockInfo.IndexedBlockHeight.Add(body?.IndexedInfo.Aggregate(new List<ulong>(), (h, i) =>
                        {
                            if (i.ChainId.Equals(sideChainId))
                                h.Add(i.Height);
                            return h;
                        }));
                    }
                
                    _logger?.Log(LogLevel.Debug, $"Parent Chain Server responsed IndexedInfo message of height {height}");
                    await responseStream.WriteAsync(res);

                    height++;
                }
            }
            catch
            {
                
            }
        }
    }
}