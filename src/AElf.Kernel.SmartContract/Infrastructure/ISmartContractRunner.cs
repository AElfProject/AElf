using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface ISmartContractRunner : ISmartContractCategoryProvider
    {
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
        
        string ContractVersion { get; }
    }
}