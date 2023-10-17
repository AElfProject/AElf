using AElf.Runtime.WebAssembly.TransactionPayment;

namespace AElf.Runtime.WebAssembly;

public class GasMeter
{
    public Weight GasLimit { get; set; }
    public Weight GasLeft { get; set; }
    public Weight GasLeftLowest { get; set; }
    public long EngineConsumed { get; set; }
}