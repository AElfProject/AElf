using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public interface ILogEventListeningService
    {
        Task Apply(Chain chain, IEnumerable<Hash> blockHashes);
    }
}