using System.Collections.Concurrent;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;
using AElf.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Domain;

namespace AElf.Kernel.SmartContract.Metadata
{
    
    public class FunctionMetadataService : IFunctionMetadataService
    {
        private readonly ConcurrentDictionary<int, ChainFunctionMetadata> _metadatas;
        public ILogger<FunctionMetadataService> Logger { get; set; }
        private readonly IFunctionMetadataManager _functionMetadataManager;

        public FunctionMetadataService(IFunctionMetadataManager functionMetadataManager)
        {
            Logger = NullLogger<FunctionMetadataService>.Instance;
            _metadatas = new ConcurrentDictionary<int, ChainFunctionMetadata>();
            _functionMetadataManager = functionMetadataManager;
        }

        public async Task DeployContract(int chainId, Address address, ContractMetadataTemplate contractMetadataTemplate)
        {
            //For each chain, ChainFunctionMetadata should be used singlethreaded
            //which means transactions that deploy contracts need to execute serially
            //TODO: find a way to mark these transaction as a same group (maybe by using "r/w account sharing data"?)
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                //TODO: remove new, get the instance from service provider
                chainFuncMetadata = _metadatas.GetOrAdd(chainId, new ChainFunctionMetadata(_functionMetadataManager));
            }
            
            //TODO: need to
            //1.figure out where to have this "contractReferences" properly and
            //2.how to implement the action's that call other contracts and
            //3.as the contract reference can be changed, need to set up the contract update accordingly, which is the functions that are not yet implemented
            await chainFuncMetadata.DeployNewContract(chainId, address, contractMetadataTemplate);
            Logger.LogInformation($"Metadata of contract {contractMetadataTemplate.FullName} are extracted successfully.");
        }

        public async Task UpdateContract(int chainId, Address address, ContractMetadataTemplate oldContractMetadataTemplate, ContractMetadataTemplate newContractMetadataTemplate)
        {
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                chainFuncMetadata = _metadatas.GetOrAdd(chainId, new ChainFunctionMetadata(_functionMetadataManager));
            }

            await chainFuncMetadata.UpdateContract(chainId, address, oldContractMetadataTemplate, newContractMetadataTemplate);
        }

        public async Task<FunctionMetadata> GetFunctionMetadata(int chainId, string addrFunctionName)
        {
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                throw new InvalidParameterException("No chainFunctionMetadata with chainId: " + chainId.DumpBase58());
            }

            return await chainFuncMetadata.GetFunctionMetadata(chainId, addrFunctionName);
        }
    }
}