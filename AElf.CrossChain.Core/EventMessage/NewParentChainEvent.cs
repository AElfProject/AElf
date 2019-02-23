namespace AElf.CrossChain.EventMessage
{
    public class NewParentChainEvent
    {
        public int ChainId { get; set; }
        public ICrossChainDataConsumer CrossChainDataConsumer { get; set; }
        public ICrossChainDataProducer CrossChainDataProducer { get; set; }
    }
}