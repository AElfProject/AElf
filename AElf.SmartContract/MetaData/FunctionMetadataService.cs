using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
using NLog;
using Org.BouncyCastle.Security;
using AElf.Common;

namespace AElf.SmartContract.Metadata
{
    [LoggerName("SmartContract")]
    public class FunctionMetadataService : IFunctionMetadataService
    {
        private IDataStore _dataStore;
        private readonly ConcurrentDictionary<Hash, ChainFunctionMetadata> _metadatas;
        private ILogger _logger;

        public FunctionMetadataService(IDataStore dataStore, ILogger logger)
        {
            _dataStore = dataStore;
            _logger = logger;
            _metadatas = new ConcurrentDictionary<Hash, ChainFunctionMetadata>();
        }

        public async Task DeployContract(Hash chainId, Address address, ContractMetadataTemplate contractMetadataTemplate)
        {
            //For each chain, ChainFunctionMetadata should be used singlethreaded
            //which means transactions that deploy contracts need to execute serially
            //TODO: find a way to mark these transaction as a same group (maybe by using "r/w account sharing data"?)
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                chainFuncMetadata = _metadatas.GetOrAdd(chainId, new ChainFunctionMetadata(_dataStore, _logger));
            }
            
            //TODO: need to
            //1.figure out where to have this "contractReferences" properly and
            //2.how to implement the action's that call other contracts and
            //3.as the contract reference can be changed, need to set up the contract update accordingly, which is the functions that are not yet implemented
            await chainFuncMetadata.DeployNewContract(chainId, address, contractMetadataTemplate);
            _logger?.Info($"Metadata of contract {contractMetadataTemplate.FullName} are extracted successfully.");
        }

        public async Task<FunctionMetadata> GetFunctionMetadata(Hash chainId, string addrFunctionName)
        {
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                throw new InvalidParameterException("No chainFunctionMetadata with chainId: " + chainId.DumpHex());
            }

            return await chainFuncMetadata.GetFunctionMetadata(chainId, addrFunctionName);
        }
    }
}