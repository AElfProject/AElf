using System;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IExecutive
    {
        IExecutive SetSmartContractContext(ISmartContractContext contractContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        Task Apply(bool autoCommit);
    }
}
