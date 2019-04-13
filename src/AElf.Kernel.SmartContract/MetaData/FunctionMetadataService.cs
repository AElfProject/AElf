using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AElf.Kernel.SmartContract.Domain;

namespace AElf.Kernel.SmartContract.Metadata
{
    public class FunctionMetadataService : IFunctionMetadataService
    {
        public ILogger<FunctionMetadataService> Logger { get; set; }
        private readonly IFunctionMetadataManager _functionMetadataManager;

        private ChainFunctionMetadata chainFuncMetadata;

        public FunctionMetadataService(IFunctionMetadataManager functionMetadataManager)
        {
            Logger = NullLogger<FunctionMetadataService>.Instance;
            chainFuncMetadata = new ChainFunctionMetadata(_functionMetadataManager);
            _functionMetadataManager = functionMetadataManager;
        }

        public async Task DeployContract(Address address, ContractMetadataTemplate contractMetadataTemplate)
        {
            //For each chain, ChainFunctionMetadata should be used singlethreaded
            //which means transactions that deploy contracts need to execute serially
            //TODO: find a way to mark these transaction as a same group (maybe by using "r/w account sharing data"?)

            //TODO: need to
            //1.figure out where to have this "contractReferences" properly and
            //2.how to implement the action's that call other contracts and
            //3.as the contract reference can be changed, need to set up the contract update accordingly, which is the functions that are not yet implemented
            await chainFuncMetadata.DeployNewContract(address, contractMetadataTemplate);
            Logger.LogInformation(
                $"Metadata of contract {contractMetadataTemplate.FullName} are extracted successfully.");
        }

        public async Task UpdateContract(Address address, ContractMetadataTemplate oldContractMetadataTemplate,
            ContractMetadataTemplate newContractMetadataTemplate)
        {
            await chainFuncMetadata.UpdateContract(address, oldContractMetadataTemplate, newContractMetadataTemplate);
        }

        public async Task<FunctionMetadata> GetFunctionMetadata(string addrFunctionName)
        {
            return await chainFuncMetadata.GetFunctionMetadata(addrFunctionName);
        }
    }
}