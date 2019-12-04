using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface IContractRemarksCacheProvider
    {
        CodeRemark GetCodeRemark(IChainContext chainContext, Address address);
        void SetCodeRemark(Address address, CodeRemark codeRemark);
        void AddCodeRemark(Address address, CodeRemark codeRemark);

        void AddCodeHashCache(IBlockIndex blockIndex, Address address, Hash codeHash);

        Hash GetCodeHash(IBlockIndex blockIndex, Address address);

        bool MayHasContractRemarks(IBlockIndex previousBlockIndex);

        Dictionary<Address, List<CodeRemark>> RemoveForkCache(List<BlockIndex> blockIndexes);
        Dictionary<Address, CodeRemark> SetIrreversedCache(List<BlockIndex> blockIndexes);
    }

    public class ContractRemarksCacheProvider : IContractRemarksCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, CodeRemark> _cache =
            new ConcurrentDictionary<Address, CodeRemark>();

        private readonly ConcurrentDictionary<Address, List<CodeRemark>> _forkCache =
            new ConcurrentDictionary<Address, List<CodeRemark>>();

        private readonly ConcurrentDictionary<IBlockIndex, List<CodeHashCache>> _codeHashCacheMappings =
            new ConcurrentDictionary<IBlockIndex, List<CodeHashCache>>();

        private readonly IChainBlockLinkCacheProvider _chainBlockLinkCacheProvider;

        public ILogger<ContractRemarksCacheProvider> Logger { get; set; }

        public ContractRemarksCacheProvider(IChainBlockLinkCacheProvider chainBlockLinkCacheProvider)
        {
            _chainBlockLinkCacheProvider = chainBlockLinkCacheProvider;
            Logger = NullLogger<ContractRemarksCacheProvider>.Instance;
        }

        public CodeRemark GetCodeRemark(IChainContext chainContext, Address address)
        {
            var blockHash = chainContext.BlockHash;
            var blockHeight = chainContext.BlockHeight;
            if (_forkCache.TryGetValue(address, out var codeRemarks))
            {
                var blockHashes = codeRemarks.Select(c => c.BlockHash).ToList();
                var minHeight = codeRemarks.Select(s => s.BlockHeight).Min();
                do
                {
                    if (blockHashes.Contains(blockHash))
                    {
                        return codeRemarks.First(c => c.BlockHash == blockHash);
                    }

                    var block = _chainBlockLinkCacheProvider.GetChainBlockLink(blockHash);
                    blockHash = block?.PreviousBlockHash;
                    blockHeight--;
                } while (blockHash != null && blockHeight >= minHeight);
            }

            _cache.TryGetValue(address, out var codeRemark);
            return codeRemark;
        }

        public void SetCodeRemark(Address address, CodeRemark codeRemark)
        {
            _cache[address] = codeRemark;
        }

        public void AddCodeRemark(Address address, CodeRemark codeRemark)
        {
            Logger.LogTrace($"Set contract remarks Address: {address}, CodeRemark:{codeRemark}");
            if (!_forkCache.TryGetValue(address, out var codeRemarks))
            {
                codeRemarks = new List<CodeRemark>();
                _forkCache[address] = codeRemarks;
            }

            codeRemarks.AddIfNotContains(codeRemark);
        }

        public void AddCodeHashCache(IBlockIndex blockIndex, Address address, Hash codeHash)
        {
            if (!_codeHashCacheMappings.TryGetValue(blockIndex, out var caches))
            {
                caches = new List<CodeHashCache>();
                _codeHashCacheMappings[blockIndex] = caches;
            }

            caches.AddIfNotContains(new CodeHashCache
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

        public Dictionary<Address, List<CodeRemark>> RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            var codeRemarkDic = new Dictionary<Address, List<CodeRemark>>();
            var addresses = _forkCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var codeRemarks = _forkCache[address];
                codeRemarkDic[address] = codeRemarks.Where(c => blockHashes.Contains(c.BlockHash)).ToList();
                codeRemarks.RemoveAll(c => blockHashes.Contains(c.BlockHash));
                if (codeRemarks.Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }

            return codeRemarkDic;
        }

        public Dictionary<Address, CodeRemark> SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            var codeRemarkDic = new Dictionary<Address, CodeRemark>();
            var addresses = _forkCache.Keys.ToList();
            var blockHashes = blockIndexes.Select(b => b.BlockHash).ToList();
            foreach (var address in addresses)
            {
                var codeRemarks = _forkCache[address].OrderBy(c => c.BlockHeight).ToList();
                foreach (var codeRemark in codeRemarks)
                {
                    if (!blockHashes.Contains(codeRemark.BlockHash)) continue;
                    _cache[address] = codeRemark;
                    _forkCache[address].Remove(codeRemark);
                    codeRemarkDic[address] = codeRemark;
                }

                if (_forkCache[address].Count != 0) continue;
                _forkCache.TryRemove(address, out _);
            }

            return codeRemarkDic;
        }
    }

    public class CodeHashCache
    {
        public Address Address { get; set; }

        public Hash CodeHash { get; set; }
    }
}