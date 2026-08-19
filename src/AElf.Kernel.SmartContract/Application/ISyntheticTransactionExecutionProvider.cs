using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application;

public interface ISyntheticTransactionExecutionProvider
{
    bool TryApply(Transaction transaction, TransactionTrace trace);
}

public class SyntheticTransactionExecutionProvider : ISyntheticTransactionExecutionProvider, ISingletonDependency
{
    private static readonly Address BypassedContractAddress =
        Address.FromBase58("tHjyUJyDGoipsHXDV4WsV7KT8mwqZus4CxTb2Vb2G7VePef7g");

    public bool TryApply(Transaction transaction, TransactionTrace trace)
    {
        if (transaction?.To != BypassedContractAddress)
            return false;

        trace.ExecutionStatus = ExecutionStatus.Executed;
        trace.Error = string.Empty;
        return true;
    }
}
