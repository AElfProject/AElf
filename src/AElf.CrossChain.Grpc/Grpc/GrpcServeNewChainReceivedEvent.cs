namespace AElf.CrossChain.Grpc
{
    public sealed class GrpcServeNewChainReceivedEvent
    {
        public int LocalChainId{ get; set; }
//        public ICrossChainDataConsumer CrossChainDataConsumer { get; set; }
//        public ICrossChainDataProducer CrossChainDataProducer { get; set; }
        
        public ICrossChainCommunicationContext CrossChainCommunicationContextDto { get; set; }
    }
}