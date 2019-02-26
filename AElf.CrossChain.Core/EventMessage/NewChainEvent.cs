namespace AElf.CrossChain.EventMessage
{
    public sealed class NewChainEvent
    {
        public int LocalChainId{ get; set; }
//        public ICrossChainDataConsumer CrossChainDataConsumer { get; set; }
//        public ICrossChainDataProducer CrossChainDataProducer { get; set; }
        
        public ICrossChainCommunicationContext CrossChainCommunicationContext { get; set; }
    }
}