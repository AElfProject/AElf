using AElf.Kernel.SmartContract;
using AElf.Sdk.CSharp;

namespace AElf.Runtime.WebAssembly.Contract;

public class WemAssemblySmartContractContext : CSharpSmartContractContext
{
    public WemAssemblySmartContractContext(ISmartContractBridgeContext smartContractBridgeContextImplementation) : base(
        smartContractBridgeContextImplementation)
    {
    }
}