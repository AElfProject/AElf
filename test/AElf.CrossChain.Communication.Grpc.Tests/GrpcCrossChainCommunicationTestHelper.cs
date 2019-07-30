using System.Collections.Generic;
using AElf.CrossChain.Cache;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainCommunicationTestHelper
    {
        public static Dictionary<int, List<IBlockCacheEntity>> CrossChainBlockDataEntityCache =
            new Dictionary<int, List<IBlockCacheEntity>>();
    }
}