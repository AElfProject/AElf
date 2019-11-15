using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class DeployedContractAddressProvider: IDeployedContractAddressProvider, ISingletonDependency
    {
        private AddressList _addressList = new AddressList();

        public ILogger<DeployedContractAddressProvider> Logger { get; set; }
        
        private bool _initialized;

        public DeployedContractAddressProvider()
        {
            Logger = new NullLogger<DeployedContractAddressProvider>();
        }

        private readonly ConcurrentDictionary<Hash, List<Address>> _forkCache =
            new ConcurrentDictionary<Hash, List<Address>>();


        public void Init(List<Address> addresses)
        {
            _initialized = true;
            _addressList.Value.AddRange(addresses);
        }

        public bool CheckContractAddress(Address address)
        {
            if (!_initialized) return true;
            return _addressList.Value.Contains(address);
        }

        public void AddDeployedContractAddress(Address address,Hash blockHash)
        {
            if (!_forkCache.TryGetValue(blockHash, out var addresses))
            {
                addresses = new List<Address>();
                _forkCache[blockHash] = addresses;
            }

            addresses.AddIfNotContains(address);
            
            //Logger.LogInformation($"# Added deployed contract address: {address}");
        }
        
        public void RemoveForkCache(List<Hash> blockHashes)
        {
            foreach (var blockHash in blockHashes)
            {
                if(!_forkCache.TryGetValue(blockHash,out _)) continue;
                _forkCache.TryRemove(blockHash, out _);
            }
        }

        public void SetIrreversedCache(List<Hash> blockHashes)
        {
            foreach (var blockHash in blockHashes)
            {
                SetIrreversedCache(blockHash);
            } 
        }

        public void SetIrreversedCache(Hash blockHash)
        {
            if (!_forkCache.TryGetValue(blockHash, out var addresses)) return;
            _addressList.Value.AddIfNotContains(addresses);
            _forkCache.TryRemove(blockHash, out _);
        }
    }
}