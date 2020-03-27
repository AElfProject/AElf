using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ILogEventProcessor
    {
        LogEvent InterestedEvent { get; }
        Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap);
    }

    public interface IBlockAcceptedLogEventProcessor : ILogEventProcessor
    {

    }

    public interface IBestChainFoundLogEventProcessor : ILogEventProcessor
    {

    }
}