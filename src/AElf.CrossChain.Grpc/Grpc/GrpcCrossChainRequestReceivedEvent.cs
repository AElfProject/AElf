namespace AElf.CrossChain.Grpc
{
    public sealed class GrpcCrossChainRequestReceivedEvent
    {
        public string RemoteIp { get; set; }
        public int RemotePort { get; set; }
        public int RemoteChainId { get; set; }
    }
}