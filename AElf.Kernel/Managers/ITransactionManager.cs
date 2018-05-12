using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface ITransactionManager
    {
        Task<IHash> AddTransactionAsync(ITransaction tx);
        Task<ITransaction> GetTransaction(Hash txId);
    }
}