namespace AElf.CrossChain
{
    public interface IResponseIndexingMessage
    {
        bool Success { get; }
        long Height { get; }
        IBlockInfo BlockInfoResult { get; }
    }
    
    public partial class ResponseSideChainBlockData : IResponseIndexingMessage
    {
        public long Height => BlockData.Height;
        public IBlockInfo BlockInfoResult => BlockData;
    }
    
    public partial class ResponseParentChainBlockData : IResponseIndexingMessage
    {
        public long Height => BlockData.Height;
        public IBlockInfo BlockInfoResult => BlockData;
    }
}