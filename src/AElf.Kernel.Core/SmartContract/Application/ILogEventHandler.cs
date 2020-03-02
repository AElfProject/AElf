using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ILogEventHandler
    {
        LogEvent InterestedEvent { get; }
        Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent);
    }
    
    public interface IBlockAcceptedLogEventHandler : ILogEventHandler
    {

    }

    public interface IBestChainFoundLogEventHandler : ILogEventHandler
    {

    }
}