using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public interface ISmartContractStore
    {
        Task InsertAsync(Hash hash, SmartContractRegistration registration);
        Task<SmartContractRegistration> GetAsync(Hash hash);
    }
}
