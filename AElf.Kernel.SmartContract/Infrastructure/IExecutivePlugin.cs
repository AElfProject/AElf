using System;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IExecutivePlugin
    {
        void AfterApply(ISmartContract smartContract, IHostSmartContractBridgeContext context,
            Func<string, object[],object> executeReadOnlyHandler);
    }
}