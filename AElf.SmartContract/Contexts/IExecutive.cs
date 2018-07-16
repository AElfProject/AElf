using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface IExecutive
    {
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetWorldStateManager(IWorldStateDictator worldStateDictator);
        void SetDataCache(Dictionary<Hash, StateCache> cache); //temporary solution to let data provider access actor's state cache
        Task Apply(bool autoCommit);
    }
}
