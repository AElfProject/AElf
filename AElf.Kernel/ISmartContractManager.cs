using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ISmartContractManager
    {
        Task<ISmartContract> GetAsync(IAccount account);
    }
}