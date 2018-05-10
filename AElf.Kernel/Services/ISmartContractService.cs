using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public interface ISmartContractService
    {
        Task<ISmartContract> GetAsync(Hash account, IChainContext context);
    }
}