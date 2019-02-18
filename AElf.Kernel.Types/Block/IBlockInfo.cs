using AElf.Common;

namespace AElf.Kernel
{
    public interface IBlockInfo
    {
        ulong Height { get; }
        int ChainId { get; }
    }
    
    public partial class SideChainBlockData : IBlockInfo
    {
        
    }
    public partial class ParentChainBlockData : IBlockInfo
    {
        public ulong Height => Root.Height;
        public int ChainId => Root.ChainId;
    }
}