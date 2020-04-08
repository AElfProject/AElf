using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: rename
    public interface ILogEventListeningService<T> where T : ILogEventProcessor
    {
        Task ProcessAsync(List<BlockExecutedSet> blockExecutedSets);
    }
}