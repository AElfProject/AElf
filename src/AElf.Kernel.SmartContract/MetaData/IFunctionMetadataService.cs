using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(Address address, ContractMetadataTemplate contractMetadataTemplate);
        Task UpdateContract(Address address, ContractMetadataTemplate oldContractMetadataTemplate, ContractMetadataTemplate newContractMetadataTemplate);
        Task<FunctionMetadata> GetFunctionMetadata(string addrFunctionName);
    }
}