using System.Threading.Tasks;
using AElf.Types;

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
    
    Task<ContractInfoDto> DeployContractAsync(int category,byte[] code);

    Task<ContractInfoDto> UpdateContractAsync(Address address,byte[] code,long blockHeight,Hash blockHash);

    Task CheckContractVersion(Address address,byte[] code,long blockHeight,Hash blockHash);
}