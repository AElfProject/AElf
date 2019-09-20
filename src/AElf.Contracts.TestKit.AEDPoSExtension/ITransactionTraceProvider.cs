using AElf.Kernel;
using AElf.Types;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    /// <summary>
    /// This interface is just for testing, which means we need not consider parallel executing.
    /// </summary>
    public interface ITransactionTraceProvider
    {
        void AddTransactionTrace(TransactionTrace trace);
        TransactionTrace GetTransactionTrace(Hash transactionId);
    }
}