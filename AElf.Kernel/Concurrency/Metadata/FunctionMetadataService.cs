using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using NLog;
using Org.BouncyCastle.Security;
using ServiceStack;

namespace AElf.Kernel.Concurrency.Metadata
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

        public async Task DeployContract(Hash chainId, Type contractType, Hash address, Dictionary<string, Hash> contractReferences)
        {
            //For each chain, ChainFunctionMetadata should be used singlethreaded
            //which means transactions that deploy contracts need to execute serially
            //TODO: find a way to mark these transaction as a same group (maybe by using "r/w account sharing data"?)
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                
                chainFuncMetadata = _metadatas.GetOrAdd(chainId,
                    new ChainFunctionMetadata(new ChainFunctionMetadataTemplate(_dataStore, chainId, _logger), _dataStore, _logger));
            }
            
            await chainFuncMetadata.Template.TryAddNewContract(contractType);
            
            //TODO: for now we don't support call other contracts, thus the reference book is empty, try to deploy a 
            //need to
            //1.figure out where to have this "contractReferences" and
            //2.how to implement the action's that call other contracts and
            //3.as the contract reference can be changed, need to set up the contract update accordingly, which is the functions that are not yet implemented
            await chainFuncMetadata.DeployNewContract(contractType.FullName, address, contractReferences);
            _logger?.Info("Metadata of contract " + contractType.FullName + " are extracted successfully");
        }

        public FunctionMetadata GetFunctionMetadata(Hash chainId, string addrFunctionName)
        {
            if (!_metadatas.TryGetValue(chainId, out var chainFuncMetadata))
            {
                throw new InvalidParameterException("No chainFunctionMetadata with chainId: " + chainId.ToHex());
            }

            return chainFuncMetadata.GetFunctionMetadata(addrFunctionName);
        }
    }
}