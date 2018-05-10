using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ISmartContractService
    {
        Task<ISmartContract> GetAsync(Hash account, IChainContext context);
    }
}