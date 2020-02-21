using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: remove
    public interface ISmartContractRegistrationCacheProvider
    {
        bool TryGetLibCache(Address address, out SmartContractRegistrationCache cache);
        void SetLibCache(Address address, SmartContractRegistrationCache cache);
        bool TryGetForkCache(Address address, out List<SmartContractRegistrationCache> cache);
        void AddSmartContractRegistration(Address address, Hash codeHash, BlockIndex blockIndex);
        Dictionary<Address, List<Hash>> RemoveForkCache(List<BlockIndex> blockIndexes);
        Dictionary<Address, List<Hash>> SetIrreversedCache(List<BlockIndex> blockIndexes);
    }
    
    public class SmartContractRegistrationCacheProvider : ISmartContractRegistrationCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, SmartContractRegistrationCache>
            _libCache =
                new ConcurrentDictionary<Address, SmartContractRegistrationCache>();

        private readonly ConcurrentDictionary<Address, List<SmartContractRegistrationCache>> _forkCache =
            new ConcurrentDictionary<Address, List<SmartContractRegistrationCache>>();

        public bool TryGetLibCache(Address address, out SmartContractRegistrationCache cache)
        {
            return _libCache.TryGetValue(address, out cache);
        }
        
        public void SetLibCache(Address address, SmartContractRegistrationCache cache)
        {
            _libCache[address] = cache;
        }

        public bool TryGetForkCache(Address address, out List<SmartContractRegistrationCache> cache)
        {
            return _forkCache.TryGetValue(address, out cache);
        }
        
        public void AddSmartContractRegistration(Address address, Hash codeHash, BlockIndex blockIndex)
        {
            var smartContractRegistrationCache = new SmartContractRegistrationCache
            {
                Address = address,
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight,
                SmartContractRegistration = new SmartContractRegistration
                {
                    CodeHash = codeHash
                }
            };
            
            //Add genesis block registration cache to lib cache directly
            if (blockIndex.BlockHeight == 1)
            {
                _libCache.TryAdd(address, smartContractRegistrationCache);
                return;
            }

            if (!_forkCache.TryGetValue(address, out var caches))
            {
                caches = new List<SmartContractRegistrationCache>();
                _forkCache[address] = caches;
            }

            caches.Add(smartContractRegistrationCache);
        }
        
        public Dictionary<Address,List<Hash>> RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            var codeHashDic = new Dictionary<Address, List<Hash>>();
            var addresses = _forkCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var caches = _forkCache[address];
                var codeHashes = caches.Where(c => blockHashes.Contains(c.BlockHash))
                    .Select(c => c.SmartContractRegistration.CodeHash).ToList();
                caches.RemoveAll(cache => blockHashes.Contains(cache.BlockHash));
                codeHashDic[address] = codeHashes;
                if (caches.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }

            return codeHashDic;
        }

        public Dictionary<Address,List<Hash>> SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            var codeHashDic = new Dictionary<Address, List<Hash>>();
            var addresses = _forkCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var caches = _forkCache[address].OrderBy(c => c.BlockHeight).ToList();
                var oldCodeHashes = new List<Hash>();
                foreach (var cache in caches)
                {
                    if (!blockHashes.Contains(cache.BlockHash) ||
                        _libCache.TryGetValue(cache.Address,
                            out var registrationCache) &&
                        registrationCache.SmartContractRegistration.CodeHash ==
                        cache.SmartContractRegistration.CodeHash)
                        continue;
                    if (registrationCache != null)
                        oldCodeHashes.Add(registrationCache.SmartContractRegistration.CodeHash);
                    _libCache[cache.Address] = cache;
                }

                _forkCache[address].RemoveAll(cache => blockHashes.Contains(cache.BlockHash));
                codeHashDic[address] = oldCodeHashes;
                if (caches.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }
            return codeHashDic;
        }
        
        
    }
    
    public class SmartContractRegistrationCache
    {
        public Address Address { get; set; }

        public SmartContractRegistration SmartContractRegistration { get; set; }

        public Hash BlockHash { get; set; }

        public long BlockHeight { get; set; }
    }
}