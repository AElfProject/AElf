using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface ITransactionManager
    {
        Task<Hash> AddTransactionAsync(ITransaction tx);
        Task<ITransaction> GetTransaction(Hash txId);
    }
}