using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Application
{
    
    //TODO: should move to SmartContract, because we just need to know the smart contract to handle the LogEvent,
    //but we don't need to know how the smart contract was executed.
    public interface ILogEventHandler
    {
        LogEvent InterestedEvent { get; }
        Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent);
    }
    
    public interface IBlockAcceptedLogEventHandler : ILogEventHandler
    {

    }

    public interface IBestChainFoundLogEventHandler : ILogEventHandler
    {
        
    }
}