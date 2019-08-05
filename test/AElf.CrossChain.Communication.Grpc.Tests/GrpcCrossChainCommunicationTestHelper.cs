using System.Collections.Concurrent;
using System.Collections.Generic;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Communication.Infrastructure;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainCommunicationTestHelper
    {
        public static List<SideChainBlockData> ServerBlockDataEntityCache =
            new List<SideChainBlockData>();

        public static List<IBlockCacheEntity> ClientBlockDataEntityCache =
            new List<IBlockCacheEntity>();

        public readonly ConcurrentDictionary<int, ICrossChainClient> GrpcCrossChainClients =
            new ConcurrentDictionary<int, ICrossChainClient>();

        public void FakeSideChainBlockDataEntityCacheOnServerSide(int height)
        {
            var blockInfoCache = new SideChainBlockData {Height = height};
            ServerBlockDataEntityCache.Add(blockInfoCache);
        }
    }
}