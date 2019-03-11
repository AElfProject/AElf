using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IExecutivePlugin
    {
        void AfterApply(ISmartContract smartContract, IHostSmartContractBridgeContext context);
    }
}