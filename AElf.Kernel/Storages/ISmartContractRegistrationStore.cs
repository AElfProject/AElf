using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ISmartContractRegistrationStore
    {
        Task<SmartContractRegistration> GetAsync(Hash chainId, Hash account);
        Task InsertAsync(SmartContractRegistration reg);
    }

    
}