namespace AElf.CrossChain.Grpc
{
    public sealed class GrpcCrossChainRequestReceivedEvent
    {
        public int LocalChainId{ get; set; }
        public ICrossChainCommunicationContext CrossChainCommunicationContextDto { get; set; }
    }
}