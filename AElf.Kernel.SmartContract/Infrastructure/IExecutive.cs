using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IExecutive
    {
        Hash ContractHash { get; set; }
        IExecutive SetMaxCallDepth(int maxCallDepth);
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetStateProviderFactory(IStateProviderFactory stateProviderFactory);
        void SetDataCache(IStateCache cache); //temporary solution to let data provider access actor's state cache
        Task Apply();
        ulong GetFee(string methodName);
        string GetJsonStringOfParameters(string methodName, byte[] paramsBytes);
        object GetReturnValue(string methodName, byte[] bytes);

    }
}
