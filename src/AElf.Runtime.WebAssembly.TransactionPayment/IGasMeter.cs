namespace AElf.Runtime.WebAssembly.TransactionPayment;

public interface IGasMeter
{
    Weight GasLimit { get; }
    Weight GasLeft { get; }
    Weight ChargeGas(RuntimeCost runtimeCost);
}