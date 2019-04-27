namespace AElf.Contracts.CrossChain
{
    public interface IBlockInfo
    {
        long Height { get; }
        int ChainId { get; }
    }
    
    public partial class SideChainBlockData : IBlockInfo
    {
        public long Height => SideChainHeight;
        public int ChainId => SideChainId;
    }
    public partial class ParentChainBlockData : IBlockInfo
    {
        public long Height => Root.ParentChainHeight;
        public int ChainId => Root.ParentChainId;
    }
}