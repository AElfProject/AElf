using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Sdk
{

    public interface IStateProvider
    {
        ITransactionContext TransactionContext { get; set; }
        IStateCache Cache { get; set; }
        Task<byte[]> GetAsync(StatePath path);
        
    }
}