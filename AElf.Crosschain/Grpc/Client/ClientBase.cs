using System.Collections.Concurrent;
using System.Linq;
using AElf.Kernel;

namespace AElf.Crosschain.Grpc.Client
{
    public class ClientBase
    {
        public string TargetIp { get; set; }
        public uint TargetPort { get; set; }
        public int TargetChainId { get; set; }
        public ulong TargetChainHeight { get; set; }
        public bool TargetIsSideChain { get; set; }
        public BlockInfoCache BlockInfoCache { get; set; }

        public string ToUriStr()
        {
            return string.Join(":",TargetIp, TargetPort);
        }

        
        public bool AddNewBlockInfo(IBlockInfo blockInfo)
        {
            if (blockInfo.Height != TargetChainHeight)
                return false;
            var res = BlockInfoCache.TryAdd(blockInfo);
            if (res)
                TargetChainHeight = blockInfo.Height + 1;
            return res;
        }
    }
}