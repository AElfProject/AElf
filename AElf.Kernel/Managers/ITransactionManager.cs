using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface ITransactionManager
    {
        Task AddTransactionAsync(Transaction tx);
    }
}