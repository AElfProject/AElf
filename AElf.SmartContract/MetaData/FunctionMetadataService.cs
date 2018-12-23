using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
using Org.BouncyCastle.Security;
using AElf.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.SmartContract.Metadata
{
    [LoggerName("SmartContract")]
    public class FunctionMetadataService : IFunctionMetadataService
    {
        private IDataStore _dataStore;
        private readonly ConcurrentDictionary<Hash, ChainFunctionMetadata> _metadatas;
        public ILogger<FunctionMetadataService> Logger { get; set; }

        public FunctionMetadataService(IDataStore dataStore)
        {
            _dataStore = dataStore;
            Logger = NullLogger<FunctionMetadataService>.Instance;
            _metadatas = new ConcurrentDictionary<Hash, ChainFunctionMetadata>();
        }

        public async Task DeployContract(Hash chainId, Address address, ContractMetadataTemplate contractMetadataTemplate)
        {
            //For each chain, ChainFunctionMetadata should be used singlethreaded
            //which means transactions that deploy contracts need to execute serially
            //TODO: find a way to mark these transaction as a same group (maybe by using "r/w account sharing data"?)
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                //TODO: remove new, get the instance from service provider
                chainFuncMetadata = _metadatas.GetOrAdd(chainId, new ChainFunctionMetadata(_dataStore));
            }
            
            
            //TODO: need to
            //1.figure out where to have this "contractReferences" properly and
            //2.how to implement the action's that call other contracts and
            //3.as the contract reference can be changed, need to set up the contract update accordingly, which is the functions that are not yet implemented
            await chainFuncMetadata.DeployNewContract(chainId, address, contractMetadataTemplate);
            Logger.LogInformation($"Metadata of contract {contractMetadataTemplate.FullName} are extracted successfully.");
        }

        public async Task UpdateContract(Hash chainId, Address address, ContractMetadataTemplate oldContractMetadataTemplate, ContractMetadataTemplate newContractMetadataTemplate)
        {
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                chainFuncMetadata = _metadatas.GetOrAdd(chainId, new ChainFunctionMetadata(_dataStore));
            }

            await chainFuncMetadata.UpdateContract(chainId, address, oldContractMetadataTemplate, newContractMetadataTemplate);
        }

        public async Task<FunctionMetadata> GetFunctionMetadata(Hash chainId, string addrFunctionName)
        {
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                throw new InvalidParameterException("No chainFunctionMetadata with chainId: " + chainId.DumpBase58());
            }

            return await chainFuncMetadata.GetFunctionMetadata(chainId, addrFunctionName);
        }
    }
}