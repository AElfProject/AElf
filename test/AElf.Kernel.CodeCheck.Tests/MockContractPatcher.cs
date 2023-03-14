using AElf.Kernel.CodeCheck.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.CodeCheck;

public class MockContractPatcher : IContractPatcher, ITransientDependency
{
    public int Category { get; } = 0;

    public byte[] Patch(byte[] code, bool isSystemContract)
    {
        return code;
    }
}