using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Kernel.SmartContract.Contexts
{
    public interface IStateProvider
    {
        ITransactionContext TransactionContext { get; set; }
        Dictionary<StatePath, StateCache> Cache { get; set; }
        Task<byte[]> GetAsync(StatePath path);
        
    }
}