using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp;

namespace AElf.Kernel.SmartContract.Application;

public interface ISmartContractService
{
    /// <summary>
    ///     Deploys a contract to the specified chain and account.
    /// </summary>
    /// <param name="contractDto"></param>
    /// <returns></returns>
    Task DeployContractAsync(ContractDto contractDto);

    Task UpdateContractAsync(ContractDto contractDto);
    
    Task<ContractInfoDto> DeployContractAsync(SmartContractRegistration registration);

    Task<ContractInfoDto> UpdateContractAsync(string previousContractVersion, SmartContractRegistration registration);

    Task<ContractVersionCheckDto> CheckContractVersionAsync(string previousContractVersion, SmartContractRegistration registration);
}