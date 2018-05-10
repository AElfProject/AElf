using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Hash account);
        Task<SmartContractRegistration> InsertAsync(SmartContractRegistration reg);
    }
}