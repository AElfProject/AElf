using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Managers;

namespace AElf.Kernel
{
    public interface IExecutive
    {
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetWorldStateManager(IWorldStateDictator worldStateDictator);
        void SetDataCache(Dictionary<Hash, byte[]> cache); //temporary solution to let data provider access actor's state cache
        Task Apply(bool autoCommit);
    }
}
