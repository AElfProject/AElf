using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Infrastructure;

public interface ISmartContractRunner : ISmartContractCategoryProvider
{
    string ContractVersion { get; }
    Task<IExecutive> RunAsync(SmartContractRegistration reg);
}