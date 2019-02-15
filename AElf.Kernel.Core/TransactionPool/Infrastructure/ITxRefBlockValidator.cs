using System.Threading.Tasks;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public interface ITxRefBlockValidator
    {
        Task ValidateAsync(int chainId, Transaction tx);
    }
}