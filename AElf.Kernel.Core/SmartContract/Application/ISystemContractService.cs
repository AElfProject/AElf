using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Domain;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISystemContractService
    {
        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(int chainId, IChainContext chainContext,
            Address address);
    }
}