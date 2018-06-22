using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using Org.BouncyCastle.Security;

namespace AElf.Kernel.Concurrency.Metadata
{
    public class FunctionMetadataService : IFunctionMetadataService
    {
        private IDataStore _dataStore;
        private readonly ConcurrentDictionary<Hash, ChainFunctionMetadata> _metadatas;

        public FunctionMetadataService(IDataStore dataStore)
        {
            _dataStore = dataStore;
            _metadatas = new ConcurrentDictionary<Hash, ChainFunctionMetadata>();
        }

        public async Task DeployContract(Hash chainId, Type contractType, Hash address, Dictionary<string, Hash> contractReferences)
        {
            //For each chain, ChainFunctionMetadata should be used singlethreaded
            //which means transactions that deploy contracts need to execute serially
            //TODO: find a way to mark these transaction as a same group (maybe by using "r/w account sharing data"?)
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                chainFuncMetadata = _metadatas.GetOrAdd(chainId,
                    new ChainFunctionMetadata(new ChainFunctionMetadataTemplate(_dataStore, chainId), _dataStore));
            }
            
            await chainFuncMetadata.Template.TryAddNewContract(contractType);
            //TODO: this will cause unnecessary restore when deploy a contract
            
            
            //TODO: for now we don't support call other contracts, thus the reference book is empty, try to deploy a 
            //need to
            //1.figure out where to have this "contractReferences" and
            //2.how to implement the action's that call other contracts and
            //3.as the contract reference can be changed, need to set up the contract update accordingly, which is the functions that are not yet implemented
            await chainFuncMetadata.DeployNewContract(contractType.Name, address, contractReferences);
        }

        public async Task<FunctionMetadata> GetFunctionMetadata(Hash chainId, string addrFunctionName)
        {
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                throw new InvalidParameterException("No chainFunctionMetadata with chainId: " + chainId.Value);
            }

            return chainFuncMetadata.GetFunctionMetadata(addrFunctionName);
        }
    }
}