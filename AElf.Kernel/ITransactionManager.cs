using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ITransactionManager
    {
        Task AddTransactionAsync(Transaction tx);
    }
}