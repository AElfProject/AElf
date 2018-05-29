using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public interface ISmartContractService
    {
        Task<IExecutive> GetExecutiveAsync(Hash account, IChainContext context);
        Task PutExecutiveAsync(Hash account, IExecutive executive);
        Task DeployContractAsync(Hash account, SmartContractRegistration registration);
    }
}