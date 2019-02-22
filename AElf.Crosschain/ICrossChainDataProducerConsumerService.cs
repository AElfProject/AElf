namespace AElf.Crosschain
{
    public interface ICrossChainDataProducerConsumerService
    {
        (ICrossChainDataConsumer, ICrossChainDataProducer) CreateConsumerProducer(CommunicationContextDto communicationContextDto);
        void UpdateRequestInterval(int interval);
    }
}