using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Grpc;
using AElf.TestBase;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationTestBase : AElfIntegratedTest<CrossChainCommunicationTestModule>
    {
        protected readonly ICrossChainCacheEntityProvider CrossChainCacheEntityProvider;
        protected readonly IBlockCacheEntityProducer BlockCacheEntityProducer;
        protected readonly GrpcCrossChainClientProvider _grpcCrossChainClientProvider;
        private readonly Dictionary<int, long> _parentChainIdHeight = new Dictionary<int, long>();

        public CrossChainCommunicationTestBase()
        {
            CrossChainCacheEntityProvider = GetRequiredService<ICrossChainCacheEntityProvider>();
            BlockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
            _grpcCrossChainClientProvider = GetRequiredService<GrpcCrossChainClientProvider>();
        }

        protected void AddFakeCacheData(Dictionary<int, List<IBlockCacheEntity>> fakeCache)
        {
            foreach (var (crossChainId, blockInfos) in fakeCache)
            {
                CrossChainCacheEntityProvider.AddChainCacheEntity(crossChainId, blockInfos.First().Height);
                foreach (var blockInfo in blockInfos)
                {
                    BlockCacheEntityProducer.TryAddBlockCacheEntity(blockInfo);
                }
            }
        }

        public void AddFakeParentChainIdHeight(int parentChainId, long height)
        {
            _parentChainIdHeight.Add(parentChainId, height);
        }

        public async Task<ICrossChainClient> CreateAndGetClient(int chainId, bool toParenChain, int port,
            int remoteChainId = 0)
        {
            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = chainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = toParenChain,
                RemoteServerHost = "localhost",
                RemoteServerPort = port
            };
            _grpcCrossChainClientProvider.CreateAndCacheClient(fakeCrossChainClient);
            var client = await _grpcCrossChainClientProvider.GetClientAsync(remoteChainId);
            return client;
        }
    }
}