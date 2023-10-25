using AElf.Runtime.CSharp;

namespace AElf.Runtime.WebAssembly;

internal class WebAssemblySmartContractProxy : CSharpSmartContractProxy
{
    internal WebAssemblySmartContractProxy(object instance, Type counterType) : base(instance, counterType)
    {
    }
}