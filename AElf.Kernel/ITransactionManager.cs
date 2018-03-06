using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ITransactionManager
    {
        Task AddTransactionAsync(ITransaction tx);
    }
}