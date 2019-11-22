using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class DeployedContractAddressProvider: IDeployedContractAddressProvider, ISingletonDependency
    {
        private AddressList _addressList = new AddressList();
        
        private readonly IChainBlockLinkService _chainBlockLinkService;

        public ILogger<DeployedContractAddressProvider> Logger { get; set; }
        
        private bool _initialized;

        public DeployedContractAddressProvider(IChainBlockLinkService chainBlockLinkService)
        {
            _chainBlockLinkService = chainBlockLinkService;

            Logger = new NullLogger<DeployedContractAddressProvider>();
        }
        private readonly ConcurrentDictionary<Address,List<BlockIndex>> _forkCache = new ConcurrentDictionary<Address,List<BlockIndex>>();


        public void Init(List<Address> addresses)
        {
            _initialized = true;
            _addressList.Value.AddRange(addresses);
        }

        public bool CheckContractAddress(IChainContext chainContext,Address address)
        {
            if (!_initialized) return true;
            if (_addressList.Value.Contains(address)) return true;
            if (!_forkCache.TryGetValue(address, out var blockIndices)) return false;
            
            var minHeight = blockIndices.Select(k => k.BlockHeight).Min();
            var blockIndex = new BlockIndex
            {
                BlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight
            };
            do
            {
                if (blockIndices.Contains(blockIndex))
                {
                    return true;
                }

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockIndex.BlockHash);
                blockIndex.BlockHash = link?.PreviousBlockHash;
                blockIndex.BlockHeight--;
            } while (blockIndex.BlockHash != null && blockIndex.BlockHeight >= minHeight);

            return false;
        }

        public void AddDeployedContractAddress(Address address,BlockIndex blockIndex)
        {
            if (!_forkCache.TryGetValue(address, out var blockIndices))
            {
                blockIndices = new List<BlockIndex>();
                _forkCache[address] = blockIndices;
            }

            blockIndices.AddIfNotContains(blockIndex);
            
            Logger.LogInformation($"# Added deployed contract address: {address}");
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            var addresses = _forkCache.Keys.ToList();
            foreach (var address in addresses)
            {
                var blockIndices = _forkCache[address];
                blockIndices.RemoveAll(blockIndexes.Contains);
                
                if (blockIndices.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            var addresses = _forkCache.Keys.ToList();
            foreach (var address in addresses)
            {
                var blockIndices = _forkCache[address];
                foreach (var blockIndex in blockIndices)
                {
                    if (!blockIndexes.Contains(blockIndex)) continue;
                    _addressList.Value.Add(address);
                }

                blockIndices.RemoveAll(blockIndexes.Contains);
                if (blockIndices.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            } 
        }
    }
}