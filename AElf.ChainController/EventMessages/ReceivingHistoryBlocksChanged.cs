namespace AElf.ChainController.EventMessages
{
    public sealed class ReceivingHistoryBlocksChanged
    {
        public bool IsReceiving { get; private set; }
        
        public ReceivingHistoryBlocksChanged(bool value)
        {
            IsReceiving = value;
        }
    }
}