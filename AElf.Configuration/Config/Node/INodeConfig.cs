namespace AElf.Configuration
{
    public interface INodeConfig
    {
        bool FullNode { get; set; }
        bool IsMiner { get; set; }
        byte[] ChainId { get; set; }
        string DataDir { get; set; }
        bool IsChainCreator { get; set; }
        bool ConsensusInfoGenerater { get; set; }
    }
}