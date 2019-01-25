using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.SmartContract.Contexts
{
    public interface IStateProvider
    {
        ITransactionContext TransactionContext { get; set; }
        ISmartContractContext ContractContext { get; set; }
        Task<byte[]> GetAsync(StatePath path);
    }
}