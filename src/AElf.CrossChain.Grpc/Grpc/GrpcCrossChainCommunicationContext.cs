namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainCommunicationContext : ICrossChainCommunicationContext
    {
        public string TargetIp { get; set; }
        public int TargetPort { get; set; }
        public int RemoteChainId { get; set; }
        public int LocalChainId { get; set; }
        public bool RemoteIsSideChain { get; set; }
        
        public int LocalListeningPort { get; set; }
        public int ConnectionTimeout { get; set; }

        public string ToUriStr()
        {
            return string.Join(":",TargetIp, TargetPort);
        }
    }
}