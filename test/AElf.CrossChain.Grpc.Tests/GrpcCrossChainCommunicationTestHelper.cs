using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Standards.ACS7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Communication.Infrastructure;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainCommunicationTestHelper
    {
        public static List<SideChainBlockData> ServerBlockDataEntityCache =
            new List<SideChainBlockData>();

        public static List<ICrossChainBlockEntity> ClientBlockDataEntityCache =
            new List<ICrossChainBlockEntity>();

        public readonly ConcurrentDictionary<int, ICrossChainClient> GrpcCrossChainClients =
            new ConcurrentDictionary<int, ICrossChainClient>();

        public void FakeSideChainBlockDataEntityCacheOnServerSide(int height)
        {
            var blockInfoCache = new SideChainBlockData {Height = height};
            ServerBlockDataEntityCache.Add(blockInfoCache);
        }
    }
}