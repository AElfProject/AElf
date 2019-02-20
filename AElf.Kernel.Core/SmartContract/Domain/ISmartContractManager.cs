using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.SmartContract.Domain
{
    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Hash contractHash);
        Task InsertAsync(SmartContractRegistration registration);
    }
}