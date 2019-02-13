using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Kernel;
using Easy.MessageHub;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Crosschain.Grpc.Server
{
    public class SideChainBlockInfoRpcServer : SideChainRpc.SideChainRpcBase
    {
        private readonly IChainService _chainService;
        public ILogger<SideChainBlockInfoRpcServer> Logger {get;set;}
        private ILightChain LightChain { get; set; }
        private ulong LibHeight { get; set; }

        public SideChainBlockInfoRpcServer(IChainService chainService)
        {
            _chainService = chainService;
            Logger = NullLogger<SideChainBlockInfoRpcServer>.Instance;
        }

        public void Init(int chainId)
        {
            LightChain = _chainService.GetLightChain(chainId);
            MessageHub.Instance.Subscribe<NewLibFound>(newFoundLib => { LibHeight = newFoundLib.Height; });
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
                    if (requestedHeight > LibHeight || LibHeight < (ulong) (GlobalConfig.BlockNumberOfEachRound * 10))
                    {
                        await responseStream.WriteAsync(new ResponseSideChainBlockData
                        {
                            Success = false
                        });
                        continue;
                    }
                    var blockHeader = await LightChain.GetHeaderByHeightAsync(requestedHeight);
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