using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Manager.Interfaces;

namespace AElf.SmartContract
{
    public interface IExecutive
    {
        Hash ContractHash { get; set; }
        IExecutive SetMaxCallDepth(int maxCallDepth);
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetStateManager(IStateManager stateManager);
        void SetDataCache(Dictionary<StatePath, StateCache> cache); //temporary solution to let data provider access actor's state cache
        Task Apply();
    }
}
