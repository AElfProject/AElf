using AElf.Contracts.CrossChain;

namespace AElf.CrossChain
{
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