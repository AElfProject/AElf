namespace AElf.Kernel
{
    public partial class TransactionReceipt
    {
        public TransactionReceipt(Transaction transaction)
        {
            TransactionId = transaction.GetHash();
            Transaction = transaction;
        }

        public bool IsExecutable => SignatureStatus == SignatureStatus.SignatureValid &&
                                  RefBlockStatus == RefBlockStatus.RefBlockValid &&
                                  TransactionStatus == TransactionStatus.UnknownTransactionStatus;

        public bool ToBeBroadCasted { get; set; } = true;
    }
}