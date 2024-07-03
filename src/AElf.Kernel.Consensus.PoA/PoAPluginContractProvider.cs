using AElf.Kernel.Plugin.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.PoA;

public class PoAPluginContractProvider : IPluginContractProvider, ITransientDependency
{
    public string GetContractName()
    {
        return "AElf.Contracts.Consensus.PoA";
    }
}