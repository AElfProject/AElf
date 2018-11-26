namespace AElf.Kernel.EventMessages
{
    // ReSharper disable once InconsistentNaming
    public sealed class DPoSTransactionGenerated
    {
        public string TransactionId { get; }

        public DPoSTransactionGenerated(string transactionId)
        {
            TransactionId = transactionId;
        }
    }
}