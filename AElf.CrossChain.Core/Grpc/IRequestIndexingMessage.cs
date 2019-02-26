namespace AElf.CrossChain
{
    public interface IResponseIndexingMessage
    {
        bool Success { get; }
        ulong Height { get; }
        IBlockInfo BlockInfoResult { get; }
    }
    
    public partial class ResponseSideChainBlockData : IResponseIndexingMessage
    {
        public ulong Height => BlockData.Height;
        public IBlockInfo BlockInfoResult => BlockData;
    }
    
    public partial class ResponseParentChainBlockData : IResponseIndexingMessage
    {
        public ulong Height => BlockData.Height;
        public IBlockInfo BlockInfoResult => BlockData;
    }
}