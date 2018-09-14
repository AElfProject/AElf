namespace AElf.Kernel
{
    public interface IBlockInfo
    {
        ulong Height { get; }
        Hash ChainId { get; }
    }
    
    public partial class SideChainBlockInfo : IBlockInfo
    {
        
    }
    public partial class ParentChainBlockInfo : IBlockInfo
    {
        
    }
}