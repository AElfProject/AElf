using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISystemContractService
    {
        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(int chainId, Address address);
    }
    
    public class SystemContractService : ISystemContractService
    {
        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(int chainId, Address address)
        {
            throw new System.NotImplementedException();
        }
    }
}