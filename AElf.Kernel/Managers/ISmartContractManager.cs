using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Address contractAddress);
        Task InsertAsync(Address contractAddress, SmartContractRegistration reg);
    }
}