using AElf.Runtime.WebAssembly.TransactionPayment;

namespace AElf.Runtime.WebAssembly;

public interface ICallType
{
}

public record Call(int CalleePtr, int ValuePtr, int? DepositPtr, Weight Weight) : ICallType;
public record DelegateCall(int CodeHashPtr) : ICallType;