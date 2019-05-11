namespace AElf.CrossChain.Grpc
{
    public sealed class GrpcCrossChainRequestReceivedEvent
    {
        public string RemoteServerHost { get; set; }
        public int RemoteServerPort { get; set; }
        public int RemoteChainId { get; set; }
    }
}