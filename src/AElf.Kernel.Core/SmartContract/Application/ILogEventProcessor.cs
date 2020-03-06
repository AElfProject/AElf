using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ILogEventProcessor
    {
        LogEvent InterestedEvent { get; }
        Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent);
    }
    
    public interface IBlockAcceptedLogEventProcessor : ILogEventProcessor
    {

    }

    public interface IBestChainFoundLogEventProcessor : ILogEventProcessor
    {

    }
}