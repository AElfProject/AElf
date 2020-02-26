using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ILogEventListeningService<T> where T : ILogEventHandler
    {
        Task ApplyAsync(IEnumerable<Block> blocks);
    }
}