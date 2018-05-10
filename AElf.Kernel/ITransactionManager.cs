using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ITransactionManager
    {
        Task AddTransactionAsync(Transaction tx);
        Task<ITransaction> GetTransactionAsync(Hash tx);
    }
}