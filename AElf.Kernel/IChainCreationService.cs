using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    /// <summary>
    /// Create a new chain never existing
    /// </summary>
    public interface IChainCreationService
    {
        Task CreateNewChainAsync(Hash chainId,ISmartContractZero smartContract);
    }
}