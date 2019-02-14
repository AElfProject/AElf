using System.Threading.Tasks;

namespace AElf.Kernel.TxMemPool
{
    public interface ITxRefBlockValidator
    {
        Task ValidateAsync(Transaction tx);
    }
}