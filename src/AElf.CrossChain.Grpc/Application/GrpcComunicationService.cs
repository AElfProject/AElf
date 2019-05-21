using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Plugin.Application;
using AElf.CrossChain.Plugin.Infrastructure;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Grpc.Application
{
    public class GrpcCommunicationService : ICrossChainCommunicationService
    {
        private readonly IGrpcCrossChainClientProvider _grpcCrossChainClientProvider;
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;
        private readonly IChainCacheEntityProvider _chainCacheEntityProvider;
        public ILogger<GrpcCommunicationService> Logger { get; set; }

        public GrpcCommunicationService(IGrpcCrossChainClientProvider crossChainClientProvider,
            IBlockCacheEntityProducer blockCacheEntityProducer, IChainCacheEntityProvider chainCacheEntityProvider)
        {
            _grpcCrossChainClientProvider = crossChainClientProvider;
            _blockCacheEntityProducer = blockCacheEntityProducer;
            _chainCacheEntityProvider = chainCacheEntityProvider;
        }


        public async Task RequestCrossChainDataFromOtherChains(IEnumerable<int> chainIds)
        {
            foreach (var chainId in chainIds)
            {
                await _grpcCrossChainClientProvider.TryGetClient(chainId, out var client)
                if (!)
                    continue;
                Logger.LogTrace($" {ChainHelpers.ConvertChainIdToBase58(chainId)}");
                var targetHeight = _chainCacheEntityProvider.GetChainCacheEntity(chainId).TargetChainHeight();
                _ = _grpcCrossChainClientProvider.RequestAsync(client,
                    c => c.RequestCrossChainDataAsync(targetHeight, _blockCacheEntityProducer));
            }
        }

        public async Task ConnectWithNewChainAsync(ICrossChainClientDto crossChainClientDto)
        {
            await _grpcCrossChainClientProvider.CreateAndCacheClientAsync(crossChainClientDto);
        }

        public async Task<ByteString> RequestChainInitializationInformationAsync(int chainId)
        {
            var client = _grpcCrossChainClientProvider.CreateClientForChainInitializationInformation(chainId);
            var chainInitializationContext =
                await _grpcCrossChainClientProvider.RequestAsync(client,
                    c => c.RequestChainInitializationContext(chainId));
            return chainInitializationContext.ToByteString();
        }
    }
}