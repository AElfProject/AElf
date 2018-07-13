using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.Execution
{
    public interface ITransactionExecutingService
    {
        Task ExecuteAsync(ITransaction tx);
    }
}