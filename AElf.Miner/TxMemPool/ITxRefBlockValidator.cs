using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Miner.TxMemPool
{
    public interface ITxRefBlockValidator
    {
        Task ValidateAsync(Transaction tx);
    }
}