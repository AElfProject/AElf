using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface IContractRemarksCacheProvider
    {
        ContractRemarks GetContractRemarks(IChainContext chainContext, Address address);
        void SetContractRemarks(ContractRemarks contractRemarks);
        void SetContractRemarks(Address address, Hash codeHash, BlockHeader blockHeader);

        void AddCodeHashCache(IBlockIndex blockIndex, Address address, Hash codeHash);

        Hash GetCodeHash(IBlockIndex blockIndex, Address address);

        bool MayHasContractRemarks(IBlockIndex previousBlockIndex);
        
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        List<ContractRemarks> SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public class ContractRemarksCacheProvider : IContractRemarksCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, ContractRemarks> _cache =
            new ConcurrentDictionary<Address, ContractRemarks>();
        private readonly ConcurrentDictionary<Address, List<ContractRemarksCache>> _forkCache =
            new ConcurrentDictionary<Address, List<ContractRemarksCache>>();

        private readonly ConcurrentDictionary<IBlockIndex, List<CodeHashCache>> _codeHashCacheMappings =
            new ConcurrentDictionary<IBlockIndex, List<CodeHashCache>>();

        private readonly IChainBlockLinkCacheProvider _chainBlockLinkCacheProvider;
        
        public ILogger<ContractRemarksCacheProvider> Logger { get; set; }

        public ContractRemarksCacheProvider(IChainBlockLinkCacheProvider chainBlockLinkCacheProvider)
        {
            _chainBlockLinkCacheProvider = chainBlockLinkCacheProvider;
        }

        public ContractRemarks GetContractRemarks(IChainContext chainContext, Address address)
        {
            var blockHash = chainContext.BlockHash;
            var blockHeight = chainContext.BlockHeight;
            if(_forkCache.TryGetValue(address, out var caches))
            {
                var blockHashes = caches.Select(c => c.BlockHash).ToList();
                var minHeight = caches.Select(s => s.BlockHeight).Min();
                do
                {
                    if (blockHashes.Contains(blockHash))
                    {
                        var cache = caches.First(c => c.BlockHash == blockHash);
                        return new ContractRemarks
                        {
                            ContractAddress = address,
                            CodeHash = cache.CodeHash,
                            NonParallelizable = cache.NonParallelizable
                        };
                    }

                    var block = _chainBlockLinkCacheProvider.GetChainBlockLink(blockHash);
                    blockHash = block?.PreviousBlockHash;
                    blockHeight--;
                } while (blockHash != null && blockHeight >= minHeight);
            }

            _cache.TryGetValue(address, out var contractRemarks);
            return contractRemarks;
        }
        
        public void SetContractRemarks(ContractRemarks contractRemarks)
        {
            _cache[contractRemarks.ContractAddress] = contractRemarks;
        }

        public void SetContractRemarks(Address address, Hash codeHash, BlockHeader blockHeader)
        {
            Logger.LogTrace($"Set contract remarks Address: {address}, CodeHash:{codeHash}, BlockHeader:{blockHeader}");
            if (!_forkCache.TryGetValue(address, out var contractRemarksCaches))
            {
                contractRemarksCaches = new List<ContractRemarksCache>();
                _forkCache[address] = contractRemarksCaches;
            }

            contractRemarksCaches.Add(new ContractRemarksCache
            {
                Address = address,
                CodeHash = codeHash,
                BlockHash = blockHeader.GetHash(),
                BlockHeight = blockHeader.Height,
                NonParallelizable = true
            });
        }

        public void AddCodeHashCache(IBlockIndex blockIndex, Address address, Hash codeHash)
        {
            if (!_codeHashCacheMappings.TryGetValue(blockIndex, out var caches))
            {
                caches = new List<CodeHashCache>();
                _codeHashCacheMappings[blockIndex] = caches;
            }

            caches.Add(new CodeHashCache
            {
                Address = address,
                CodeHash = codeHash
            });
        }

        public Hash GetCodeHash(IBlockIndex blockIndex, Address address)
        {
            _codeHashCacheMappings.TryGetValue(blockIndex, out var caches);
            return caches?.First(c => c.Address == address).CodeHash;
        }

        public bool MayHasContractRemarks(IBlockIndex previousBlockIndex)
        {
            return _codeHashCacheMappings.TryGetValue(previousBlockIndex, out _);
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            var addresses = _forkCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var caches = _forkCache[address];
                caches?.RemoveAll(c => blockHashes.Contains(c.BlockHash));
                if (caches?.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }
        }

        public List<ContractRemarks> SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            var contractRemarksList = new List<ContractRemarks>();
            var addresses = _forkCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var caches = _forkCache[address];
                foreach (var cache in caches)
                {
                    if(!blockHashes.Contains(cache.BlockHash)) continue;
                    var contractRemarks = new ContractRemarks
                    {
                        ContractAddress = address,
                        CodeHash = cache.CodeHash,
                        NonParallelizable = true
                    };
                    _cache[address] = contractRemarks;
                    contractRemarksList.Add(contractRemarks);
                    caches.Remove(cache);
                }
                if (caches.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }

            return contractRemarksList;
        }
    }
    
    public class ContractRemarksCache
    {
        public Address Address { get; set; }
        
        public Hash CodeHash { get; set; }
        
        public Hash BlockHash { get; set; }
        
        public long BlockHeight { get; set; } 
        
        public bool NonParallelizable { get; set; }
    }
    
    public class CodeHashCache
    {
        public Address Address { get; set; }
        
        public Hash CodeHash { get; set; }
    }
}