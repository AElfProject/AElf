using System.Reflection;

namespace AElf.Kernel
{
    public partial class TransactionReceipt
    {
        public TransactionReceipt(Transaction transaction)
        {
            TransactionId = transaction.GetHash();
            Transaction = transaction;
        }

        public bool IsExecutable => SignatureSt == Types.SignatureStatus.SignatureValid &&
                                  RefBlockSt == Types.RefBlockStatus.RefBlockValid &&
                                  Status == Types.TransactionStatus.UnknownTransactionStatus;
    }
}