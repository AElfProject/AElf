using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ISmartContractStore
    {
        Task InsertAsync(Hash hash, SmartContractRegistration registration);
        Task<SmartContractRegistration> GetAsync(Hash hash);
    }
}
