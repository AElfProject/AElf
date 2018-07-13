using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface IExecutive
    {
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        IExecutive SetWorldStateManager(IWorldStateDictator worldStateDictator);
        Task Apply(bool autoCommit);
    }
}
