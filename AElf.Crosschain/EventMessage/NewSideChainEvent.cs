namespace AElf.Crosschain.EventMessage
{
    public sealed class NewSideChainEvent
    {
        public int ChainId { get; set; }
        public ICrossChainDataConsumer CrossChainDataConsumer { get; set; }
        public ICrossChainDataProducer CrossChainDataProducer { get; set; }
    }
}