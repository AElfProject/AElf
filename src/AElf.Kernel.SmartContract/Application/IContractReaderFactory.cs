using AElf.CSharp.Core;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IContractReaderFactory
    {
        T Create<T>(ContractReaderContext contractReaderContext)
            where T : ContractStubBase, new();
    }
}