namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainCommunicationContext : ICrossChainCommunicationContext
    {
        public string TargetIp { get; set; }
        public uint TargetPort { get; set; }
        public int RemoteChainId { get; set; }
        public ulong TargetChainHeight { get; set; }
        public bool IsSideChain { get; set; }
        public int ChainId { get; set; }
        
        public string ToUriStr()
        {
            return string.Join(":",TargetIp, TargetPort);
        }
    }
}