using AElf.Kernel.SmartContractExecution.Domain;

namespace AElf.SmartContract.Contexts
{
    public interface IStateProviderFactory
    {
        IStateProvider CreateStateProvider();

    }
}