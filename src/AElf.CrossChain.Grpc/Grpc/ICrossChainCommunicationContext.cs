namespace AElf.CrossChain.Grpc
{
    public interface ICrossChainCommunicationContext
    {
        string TargetIp { get; set; }
        int TargetPort { get; set; }
        int RemoteChainId { get; set; }
        
        int LocalChainId { get; set; }
        bool RemoteIsSideChain { get; set; }
    }
}