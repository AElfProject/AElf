using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Hash account);
        Task InsertAsync(Hash address, SmartContractRegistration reg);
    }
}