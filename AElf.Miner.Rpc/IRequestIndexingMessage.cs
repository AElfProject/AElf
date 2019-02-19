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
    
}