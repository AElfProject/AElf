using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Kernel.CodeCheck.Infrastructure
{
    public interface IContractAuditor : ISmartContractCategoryProvider
    {
        void Audit(byte[] code, RequiredAcs requiredAcs);
    }
}