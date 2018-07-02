using System.Threading.Tasks;
using AElf.Kernel.Managers;

namespace AElf.Kernel
{
    public interface IExecutive
    {
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetWorldStateManager(IWorldStateDictator worldStateDictator);
        Task Apply(bool autoCommit);
    }
}
