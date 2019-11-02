using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ILogEventListeningService
    {
        Task ApplyAsync(IEnumerable<Hash> blockHashes);
    }
}