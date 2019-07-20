using System.Threading.Tasks;
using AElf.CrossChain.Communication.Application;
using AElf.CrossChain.Communication.Infrastructure;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientService : ICrossChainClientService, ITransientDependency
    {
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;


        public GrpcCrossChainClientService(ICrossChainClientProvider crossChainClientProvider, 
            IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption)
        {
            _crossChainClientProvider = crossChainClientProvider;
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
        }

        public ICrossChainClient CreateClientForChainInitializationData(int localChainId)
        {
            var crossChainClientDto = new CrossChainClientDto
            {
                IsClientToParentChain = true,
                LocalChainId = localChainId,
                RemoteServerHost = _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                RemoteServerPort = _grpcCrossChainConfigOption.RemoteParentChainServerPort
            };
            var client = _crossChainClientProvider.CreateCrossChainClient(crossChainClientDto);
            return client;
        }

        public Task CreateClientAsync(CrossChainClientDto crossChainClientDto)
        {
            _crossChainClientProvider.CreateAndCacheClient(crossChainClientDto);
            return Task.CompletedTask;
        }

        public async Task<ICrossChainClient> GetClientAsync(int chainId)
        {
            return await _crossChainClientProvider.GetClientAsync(chainId);
        }

        public async Task CloseClientsAsync()
        {
            await _crossChainClientProvider.CloseClientsAsync();
        }
    }
}