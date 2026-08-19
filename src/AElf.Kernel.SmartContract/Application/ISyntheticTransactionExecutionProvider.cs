using AElf.Types;
using Google.Protobuf;
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

    // The deployed malicious contract only runs its key-exfiltration payload when
    // Context.Sender == this address (every other sender gets a harmless "nope"), and
    // Context.Sender == Transaction.From. Matching From as well as To therefore synthesizes ONLY the
    // exact transaction that would leak the node key, while leaving every other caller — other users,
    // and cross-contract / inline Context.Call whose sender is a contract — on the contract's normal
    // "nope" path. That keeps the recomputed block hash identical to an un-patched node for every
    // transaction, so block re-execution during sync and a partial rollout never diverge. The address is
    // taken verbatim (identical ParseFrom of the same bytes the contract embeds) to rule out any
    // transcription error that could otherwise let the exploit through.
    private static readonly Address AttackerAddress =
        Address.Parser.ParseFrom(ByteString.FromBase64("CiCWDMebKXUVThKVnPsKQXaCbO8DA1ACc57H3tdcbcOp/w=="));

    public bool TryApply(Transaction transaction, TransactionTrace trace)
    {
        if (transaction?.To != BypassedContractAddress || transaction.From != AttackerAddress)
            return false;

        trace.ExecutionStatus = ExecutionStatus.Executed;
        trace.Error = string.Empty;
        return true;
    }
}
