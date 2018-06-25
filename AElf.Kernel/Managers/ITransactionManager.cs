using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface ITransactionManager
    {
        Task<IHash> AddTransactionAsync(ITransaction tx);
        Task<ITransaction> GetTransaction(Hash txId);
    }
}