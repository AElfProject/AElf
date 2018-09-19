using AElf.Kernel;

namespace AElf.Miner.Rpc
{
    public interface IRequestIndexingMessage
    {
        ulong NextHeight { get; set; }
    }
    
    public interface IResponseIndexingMessage
    {
        bool Success { get; }
        ulong Height { get;}
        IBlockInfo BlockInfoResult { get; }
    }
    public partial class RequestBlockInfo : IRequestIndexingMessage
    {
        
    }
    
    public partial class ResponseSideChainBlockInfo : IResponseIndexingMessage
    {
        public ulong Height => BlockInfo.Height;
        public IBlockInfo BlockInfoResult => BlockInfo;
    }
    
    public partial class ResponseParentChainBlockInfo : IResponseIndexingMessage
    {
        public ulong Height => BlockInfo.Height;
        public IBlockInfo BlockInfoResult => BlockInfo;
    }
}