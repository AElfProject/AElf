using AElf.Common;

namespace AElf.Kernel
{
    public interface IBlockInfo
    {
        ulong Height { get; }
        Hash ChainId { get; }
    }
    
    public partial class SideChainBlockData : IBlockInfo
    {
        
    }
    public partial class ParentChainBlockData : IBlockInfo
    {
        public ulong Height => Root.Height;
        public Hash ChainId => Root.ChainId;
    }
}