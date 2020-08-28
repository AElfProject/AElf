using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Kernel.CodeCheck.Infrastructure
{
    public interface IContractAuditor : ISmartContractCategoryProvider
    {
        void Audit(byte[] code, RequiredAcs requiredAcs, bool isSystemContract);
    }

    public interface IContractPatcher : ISmartContractCategoryProvider
    {
        byte[] Patch(byte[] code, bool isSystemContract);
    }
}