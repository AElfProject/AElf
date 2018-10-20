using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Storages;

namespace AElf.SmartContract
{
    public interface IExecutive
    {
        IExecutive SetMaxCallDepth(int maxCallDepth);
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetStateStore(IStateStore stateStore);
        void SetDataCache(Dictionary<DataPath, StateCache> cache); //temporary solution to let data provider access actor's state cache
        Task Apply();
    }
}
