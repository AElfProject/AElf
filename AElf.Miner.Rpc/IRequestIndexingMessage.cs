namespace AElf.Miner.Rpc
{
    public interface IRequestIndexingMessage
    {
        ulong NextHeight { get; set; }
    }
    
    public interface IResponseIndexingMessage
    {
        bool Success { get; set; }
        ulong Height { get; set; }
    }
    public partial class RequestSideChainIndexingInfo : IRequestIndexingMessage
    {
        
    }
    
    public partial class ResponseSideChainIndexingInfo : IResponseIndexingMessage
    {
        
    }
    
    public partial class RequestParentChainIndexingInfo : IRequestIndexingMessage
    {
        
    }
    
    public partial class ResponseParentChainIndexingInfo : IResponseIndexingMessage
    {
        
    }
}