using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ISmartContractRegistrationStore
    {
        Task InsertAsync(Hash hash, SmartContractRegistration registration);
        Task<SmartContractRegistration> GetAsync(Hash hash);
    }
}
