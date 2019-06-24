using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public interface ILogEventHandler
    {
        LogEvent InterestedEvent { get; }
        Task Handle(Block block, TransactionResult result, LogEvent log);
    }
}