using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Hash contractAddress);
        Task InsertAsync(Hash contractAddress, SmartContractRegistration reg);
    }
}