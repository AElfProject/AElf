using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.SmartContract.Contexts;

namespace AElf.SmartContract
{
    public interface IExecutive
    {
        Hash ContractHash { get; set; }
        IExecutive SetMaxCallDepth(int maxCallDepth);
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetStateProviderFactory(IStateProviderFactory stateProviderFactory);
        void SetDataCache(Dictionary<StatePath, StateCache> cache); //temporary solution to let data provider access actor's state cache
        Task Apply();
        ulong GetFee(string methodName);
        string GetJsonStringOfParameters(string methodName, byte[] paramsBytes);

    }
}
