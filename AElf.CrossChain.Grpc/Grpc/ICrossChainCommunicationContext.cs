namespace AElf.CrossChain
{
    public interface ICrossChainCommunicationContext
    {
        string TargetIp { get; set; }
        uint TargetPort { get; set; }
        int RemoteChainId { get; set; }
        ulong TargetChainHeight { get; set; }
        bool IsSideChain { get; set; }
        int ChainId { get; set; }
    }
}