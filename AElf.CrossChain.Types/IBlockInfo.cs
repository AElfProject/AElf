namespace AElf.CrossChain
{
    public interface IBlockInfo
    {
        ulong Height { get; }
        int ChainId { get; }
    }
    
    public partial class SideChainBlockData : IBlockInfo
    {
        public ulong Height => SideChainHeight;
        public int ChainId => SideChainId;
    }
    public partial class ParentChainBlockData : IBlockInfo
    {
        public ulong Height => Root.ParentChainHeight;
        public int ChainId => Root.ParentChainId;
    }
}