using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ILogEventListeningService<T> where T : ILogEventHandler
    {
        Task ApplyAsync(IEnumerable<Block> blocks);
    }
}