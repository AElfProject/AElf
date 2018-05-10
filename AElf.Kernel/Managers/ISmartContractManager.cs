using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Hash account);
        Task<SmartContractRegistration> InsertAsync(SmartContractRegistration reg);
    }
}