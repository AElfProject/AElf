using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AElf.Kernel.CodeCheck.Infrastructure;

public interface IContractPatcherContainer
{
    bool TryGetContractPatcher(int category, out IContractPatcher contractPatcher);
}

public class ContractPatcherContainer : IContractPatcherContainer
{
    private readonly ConcurrentDictionary<int, IContractPatcher> _contractPatchers = new();

    public ContractPatcherContainer(IEnumerable<IContractPatcher> contractPatchers)
    {
        foreach (var patcher in contractPatchers) _contractPatchers[patcher.Category] = patcher;
    }

    public bool TryGetContractPatcher(int category, out IContractPatcher contractPatcher)
    {
        return _contractPatchers.TryGetValue(category, out contractPatcher);
    }
}