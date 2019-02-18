using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Services;
using Easy.MessageHub;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Crosschain.Grpc.Server
{
    public class SideChainBlockInfoRpcServer : SideChainRpc.SideChainRpcBase
    {
        public ILogger<SideChainBlockInfoRpcServer> Logger {get;set;}
        public ILocalEventBus LocalEventBus { get; }
        private readonly IBlockchainService _blockchainService;

        public SideChainBlockInfoRpcServer(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
            Logger = NullLogger<SideChainBlockInfoRpcServer>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
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

        /// <summary>
        /// Response to indexing request from main chain node.
        /// One request to many responses. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /*public override async Task IndexServerStreaming(RequestCrossChainBlockData request, 
            IServerStreamWriter<ResponseSideChainBlockData> responseStream, ServerCallContext context)
        {
            // TODO: verify the from address and the chain 
            Logger.LogDebug("Side Chain Server received IndexedInfo message.");

            try
            {
                var height = request.NextHeight;
                while (height <= await LightChain.GetCurrentBlockHeightAsync())
                {
                    var blockHeader = await LightChain.GetHeaderByHeightAsync(height);
                    var res = new ResponseSideChainBlockData
                    {
                        Success = blockHeader != null,
                        BlockData = blockHeader == null ? null : new SideChainBlockData
                        {
                            Height = height,
                            BlockHeaderHash = blockHeader.GetHash(),
                            TransactionMKRoot = blockHeader.MerkleTreeRootOfTransactions,
                            ChainId = blockHeader.ChainId
                        }
                    };
                    //Logger.LogLog(LogLevel.Debug, $"Side Chain Server responsed IndexedInfo message of height {height}");
                    await responseStream.WriteAsync(res);
                    height++;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while index server streaming.");
            }
        }*/
    }
}