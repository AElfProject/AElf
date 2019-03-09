namespace AElf.CrossChain
{
    public interface ICrossChainCommunicationContext
    {
        string TargetIp { get; set; }
        int TargetPort { get; set; }
        int RemoteChainId { get; set; }
        
        int SelfChainId { get; set; }
        bool RemoteIsSideChain { get; set; }
    }
}