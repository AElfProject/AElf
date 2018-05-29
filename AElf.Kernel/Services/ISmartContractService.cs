using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public interface ISmartContractService
    {
        Task<IExecutive> GetAsync(Hash account, IChainContext context);
        Task PutAsync(Hash account, IExecutive executive);
    }
}