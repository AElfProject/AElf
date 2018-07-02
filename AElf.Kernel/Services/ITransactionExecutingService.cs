using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public interface ITransactionExecutingService
    {
        Task ExecuteAsync(ITransaction tx);
    }
}