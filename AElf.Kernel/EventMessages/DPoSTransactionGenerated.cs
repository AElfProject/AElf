namespace AElf.Kernel.EventMessages
{
    public sealed class DPoSTransactionGenerated
    {
        public string TransactionId { get; }

        public DPoSTransactionGenerated(string transactionId)
        {
            TransactionId = transactionId;
        }
    }
}