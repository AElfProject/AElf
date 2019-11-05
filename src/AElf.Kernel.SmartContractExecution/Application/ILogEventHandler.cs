using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ILogEventHandler
    {
        LogEvent InterestedEvent { get; }
        Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent);
    }
}