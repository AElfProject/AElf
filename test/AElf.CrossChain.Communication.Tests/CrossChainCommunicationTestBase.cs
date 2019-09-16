using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Application;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.Kernel;
using AElf.TestBase;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationTestBase : AElfIntegratedTest<CrossChainCommunicationTestModule>
    {
        protected readonly ICrossChainCacheEntityProvider CrossChainCacheEntityProvider;
        protected readonly IBlockCacheEntityProducer BlockCacheEntityProducer;
        protected readonly ICrossChainClientProvider _grpcCrossChainClientProvider;
        private readonly Dictionary<int, long> _parentChainIdHeight = new Dictionary<int, long>();
        protected ChainOptions _chainOptions;

        public CrossChainCommunicationTestBase()
        {
            CrossChainCacheEntityProvider = GetRequiredService<ICrossChainCacheEntityProvider>();
            BlockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
            _grpcCrossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
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

        public  ICrossChainClient CreateAndGetClient(int chainId, bool toParenChain,
            int remoteChainId = 0)
        {
            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = chainId,
                RemoteChainId = _chainOptions.ChainId,
                IsClientToParentChain = toParenChain,
                RemoteServerHost = "localhost",
                RemoteServerPort = 5000
            };
            var client = _grpcCrossChainClientProvider.AddOrUpdateClient(fakeCrossChainClient);
            return client;
        }
    }
}