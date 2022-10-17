using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using Orleans.Runtime;

namespace AElf.Kernel.Orleans.Core;

public class SmartContractGrain : ISmartContractGrain
{
    private readonly IPersistentState<IExecutive> _executiveState;

    public SmartContractGrain(
        [PersistentState("executive", "Executive")]
        IPersistentState<IExecutive> executiveState)
    {
        _executiveState = executiveState;
    }

    public Task SetExecutiveAsync(IExecutive executive)
    {
        if (_executiveState.State == null || _executiveState.State.ContractVersion == executive.ContractVersion)
        {
            return Task.CompletedTask;
        }

        if (new Version(_executiveState.State.ContractVersion) < new Version(executive.ContractVersion))
        {
            _executiveState.State = executive;
        }

        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(TransactionContext transactionContext)
    {
        await _executiveState.State.ApplyAsync(transactionContext);
    }
}