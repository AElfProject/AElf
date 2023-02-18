using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

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

    Task<ByteString> GetSmartContractCodeAsync(Hash originCodeHash);

    Task AddSmartContractCodeAsync(Hash originCodeHash, ByteString patchedCode);
}