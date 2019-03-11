using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.SmartContractExecution.Domain;

namespace AElf.Kernel.SmartContract.Contexts
{
    public interface IStateProviderFactory
    {
        IStateProvider CreateStateProvider();

    }
}