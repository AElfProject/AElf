using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Execution;
using Akka.Actor;

namespace AElf.Kernel.Concurrency
{
    public interface IConcurrencyExecutingService
    {
        Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId);

        void InitWorkActorSystem(string ip, int port);

    }
}