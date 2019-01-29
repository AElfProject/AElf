using System.Collections.Concurrent;
using AElf.Kernel;

namespace AElf.Crosschain.Grpc.Client
{
    public class BlockInfoCache
    {
        private BlockingCollection<IBlockInfo> ToBeIndexedBlockInfoQueue { get;} =
            new BlockingCollection<IBlockInfo>(new ConcurrentQueue<IBlockInfo>());

        private BlockingCollection<IBlockInfo> CachedIndexedBlockInfoQueue { get;} =
            new BlockingCollection<IBlockInfo>(new ConcurrentQueue<IBlockInfo>());
        
        public bool TryAdd(IBlockInfo blockInfo)
        {
            return ToBeIndexedBlockInfoQueue.TryAdd(blockInfo);    
        }
    }
}