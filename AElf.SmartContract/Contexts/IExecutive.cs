using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface IExecutive
    {
        IExecutive SetMaxCallDepth(int maxCallDepth);
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetStateDictator(IStateDictator stateDictator);
        void SetDataCache(Dictionary<DataPath, StateCache> cache); //temporary solution to let data provider access actor's state cache
        Task Apply();
    }
}
