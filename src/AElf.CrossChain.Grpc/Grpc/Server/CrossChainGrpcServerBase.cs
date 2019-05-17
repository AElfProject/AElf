using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
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

        public override async Task RequestIndexingFromParentChainAsync(CrossChainRequest crossChainRequest, 
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
            
            PublishCrossChainRequestReceivedEvent(context.Host, crossChainRequest.ListeningPort, crossChainRequest.FromChainId);
        }
        
        public override async Task RequestIndexingFromSideChainAsync(CrossChainRequest crossChainRequest, 
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
            
            PublishCrossChainRequestReceivedEvent(context.Host, crossChainRequest.ListeningPort, crossChainRequest.FromChainId);
        }

        public override Task<HandShakeReply> CrossChainIndexingShakeAsync(HandShake request, ServerCallContext context)
        {
            Logger.LogTrace($"Received shake from chain {ChainHelpers.ConvertChainIdToBase58(request.FromChainId)}.");
            PublishCrossChainRequestReceivedEvent(context.Host, request.ListeningPort, request.FromChainId);
            return Task.FromResult(new HandShakeReply {Result = true});
        }

        public override async Task<SideChainInitializationInformation> RequestChainInitializationContextFromParentChainAsync(SideChainInitializationRequest request, ServerCallContext context)
        {
            var libDto = await _blockchainService.GetLibHashAndHeightAsync();
            var sideChainInitializationResponse =
                await _parentChainServerService.GetChainInitializationContextAsync(request.ChainId, libDto);
            return sideChainInitializationResponse;
        }
        
        private void PublishCrossChainRequestReceivedEvent(string peer, int port, int chainId)
        {
            var host = new UriBuilder(peer).Host;
            LocalEventBus.PublishAsync(new GrpcCrossChainRequestReceivedEvent
            {
                RemoteServerHost = host,
                RemoteServerPort = port,
                RemoteChainId = chainId
            });
        }
    }
}