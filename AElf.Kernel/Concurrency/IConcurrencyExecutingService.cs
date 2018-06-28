using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Scheduling;

namespace AElf.Kernel.Concurrency
{
    public interface IConcurrencyExecutingService
    {
        Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId, IGrouper grouper);

        void InitWorkActorSystem();

        void InitActorSystem();
    }
}