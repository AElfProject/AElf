using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ITransactionExecutingService
    {
        Task ExecuteAsync(ITransaction tx, IChainContext chain);
    }
}