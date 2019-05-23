using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Communication;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCommunicationController : ICrossChainCommunicationController
    {
        private readonly IEnumerable<IGrpcCrossChainPlugin> _grpcCrossChainPlugins;

        public GrpcCommunicationController(IEnumerable<IGrpcCrossChainPlugin> grpcCrossChainPlugins)
        {
            _grpcCrossChainPlugins = grpcCrossChainPlugins;
        }

        public async Task StartAsync(int chainId)
        {
            foreach (var grpcCrossChainPlugin in _grpcCrossChainPlugins)
            {
                await grpcCrossChainPlugin.StartAsync(chainId);
            }
        }

        public async Task StopAsync()
        {
            foreach (var grpcCrossChainPlugin in _grpcCrossChainPlugins)
            {
                await grpcCrossChainPlugin.StopAsync();
            }
        }
    }
}