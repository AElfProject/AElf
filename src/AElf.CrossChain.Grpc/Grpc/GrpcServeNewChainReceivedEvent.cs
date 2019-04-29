namespace AElf.CrossChain.Grpc
{
    public sealed class GrpcServeNewChainReceivedEvent
    {
        public int LocalChainId{ get; set; }
        public ICrossChainCommunicationContext CrossChainCommunicationContextDto { get; set; }
    }
}