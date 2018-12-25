using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Manager.Interfaces
{
    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Address contractAddress);
        Task InsertAsync(Address contractAddress, SmartContractRegistration reg);
    }
}