using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Grpc
{
    public class CrossChainGrpcServerBase : CrossChainRpc.CrossChainRpcBase, ISingletonDependency
    {
        public ILogger<CrossChainGrpcServerBase> Logger { get; set; }
        public ILocalEventBus LocalEventBus { get; set; }
        private readonly IBlockchainService _blockchainService;
        private readonly IParentChainServerService _parentChainServerService;
        private readonly ISideChainServerService _sideChainServerService;
        
        public CrossChainGrpcServerBase(IBlockchainService blockchainService, 
            IParentChainServerService parentChainServerService, ISideChainServerService sideChainServerService)
        {
            _blockchainService = blockchainService;
            _parentChainServerService = parentChainServerService;
            _sideChainServerService = sideChainServerService;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public override async Task RequestIndexingFromParentChain(CrossChainRequest crossChainRequest, 
            IServerStreamWriter<CrossChainResponse> responseStream, ServerCallContext context)
        {
            Logger.LogTrace(
                $"Parent Chain Server received IndexedInfo message from chain {ChainHelpers.ConvertChainIdToBase58(crossChainRequest.FromChainId)}.");
            var requestedHeight = crossChainRequest.NextHeight;
            var remoteChainId = crossChainRequest.FromChainId;
            while (true)
            {
                var block = await _blockchainService.GetIrreversibleBlockByHeightAsync(requestedHeight);
                if (block == null)
                    break;
                var res = await _parentChainServerService.GenerateResponseAsync(block, remoteChainId);
                await responseStream.WriteAsync(res);
                requestedHeight++;
            }
            
            PublishCrossChainRequestReceivedEvent(context.Peer, crossChainRequest.ListeningPort, crossChainRequest.FromChainId);
        }
        
        public override async Task RequestIndexingFromSideChain(CrossChainRequest crossChainRequest, 
            IServerStreamWriter<CrossChainResponse> responseStream, ServerCallContext context)
        {
            Logger.LogTrace("Side Chain Server received IndexedInfo message.");
            var requestedHeight = crossChainRequest.NextHeight;
            while (true)
            {
                var block = await _blockchainService.GetIrreversibleBlockByHeightAsync(requestedHeight);
                if (block == null)
                    break;
                await responseStream.WriteAsync(_sideChainServerService.GenerateResponse(block));
                requestedHeight++;
            }
            
            PublishCrossChainRequestReceivedEvent(context.Peer, crossChainRequest.ListeningPort, crossChainRequest.FromChainId);
        }

        public override Task<HandShakeReply> CrossChainIndexingShake(HandShake request, ServerCallContext context)
        {
            Logger.LogTrace($"Received shake from chain {ChainHelpers.ConvertChainIdToBase58(request.FromChainId)}.");
            PublishCrossChainRequestReceivedEvent(context.Peer, request.ListeningPort, request.FromChainId);
            return Task.FromResult(new HandShakeReply{Result = true});
        }

        public override async Task<SideChainInitializationContext> RequestChainInitializationContextFromParentChain(SideChainInitializationRequest request, ServerCallContext context)
        {
            var libDto = await _blockchainService.GetLibHashAndHeight();
            var sideChainInitializationResponse =
                await _parentChainServerService.GetChainInitializationContextAsync(request.ChainId, libDto);
            return sideChainInitializationResponse;
        }
        
        private void PublishCrossChainRequestReceivedEvent(string peerContext, int port, int chainId)
        {
            var ip = peerContext.Split(':')[1];
            LocalEventBus.PublishAsync(new GrpcCrossChainRequestReceivedEvent
            {
                RemoteIp = ip,
                RemotePort = port,
                RemoteChainId = chainId
            });
        }
    }
}