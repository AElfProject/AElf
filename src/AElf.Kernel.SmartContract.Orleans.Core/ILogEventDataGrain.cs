using AElf.Kernel.SmartContract.Events;
using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public interface ILogEventDataGrain : IGrainWithIntegerKey
{
    Task ProcessLogEventAsync(LogEventContextData eventContextData);
}