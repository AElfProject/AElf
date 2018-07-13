using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.ChainController.Execution;
using AElf.SmartContract;

namespace AElf.Execution
{
    public interface IConcurrencyExecutingService
    {
        Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId, IGrouper grouper);

        void InitWorkActorSystem();

        void InitActorSystem();
    }
}