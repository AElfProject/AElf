using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.Standards.ACS7;

namespace AElf.CrossChain.Grpc;

public class GrpcCrossChainCommunicationTestHelper
{
    public static List<SideChainBlockData> ServerBlockDataEntityCache = new();

    public static List<ICrossChainBlockEntity> ClientBlockDataEntityCache = new();

    public readonly ConcurrentDictionary<int, ICrossChainClient> GrpcCrossChainClients = new();

    public void FakeSideChainBlockDataEntityCacheOnServerSide(int height)
    {
        var blockInfoCache = new SideChainBlockData { Height = height };
        ServerBlockDataEntityCache.Add(blockInfoCache);
    }
}