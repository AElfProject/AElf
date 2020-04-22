using AElf.CSharp.Core;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IContractReaderFactory<T>
        where T : ContractStubBase, new()
    {
        T Create(ContractReaderContext contractReaderContext);
    }
}