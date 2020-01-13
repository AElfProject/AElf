using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractRegistrationService : ISmartContractRegistrationService, ITransientDependency
    {
        private readonly ISmartContractRegistrationCacheProvider _smartContractRegistrationCacheProvider;
        private readonly ISmartContractCodeHistoryService _smartContractCodeHistoryService;
        private readonly IChainBlockLinkService _chainBlockLinkService;

        public SmartContractRegistrationService(ISmartContractRegistrationCacheProvider smartContractRegistrationCacheProvider,
            ISmartContractCodeHistoryService smartContractCodeHistoryService, 
            IChainBlockLinkService chainBlockLinkService)
        {
            _smartContractRegistrationCacheProvider = smartContractRegistrationCacheProvider;
            _smartContractCodeHistoryService = smartContractCodeHistoryService;
            _chainBlockLinkService = chainBlockLinkService;
        }

        public async Task AddSmartContractRegistrationAsync(Address address, Hash codeHash, BlockIndex blockIndex)
        {
            _smartContractRegistrationCacheProvider.AddSmartContractRegistration(address, codeHash, blockIndex);
            await _smartContractCodeHistoryService.AddSmartContractCodeAsync(address, codeHash, blockIndex);
        }

        public async Task<Dictionary<Address, List<Hash>>> RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            await _smartContractCodeHistoryService.RemoveAsync(blockIndexes);
            return _smartContractRegistrationCacheProvider.RemoveForkCache(blockIndexes);
        }

        public Dictionary<Address, List<Hash>> SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            return _smartContractRegistrationCacheProvider.SetIrreversedCache(blockIndexes);
        }

        public SmartContractRegistrationCache GetSmartContractRegistrationCacheFromForkCache(IChainContext chainContext,
            Address address)
        {
            if (!_smartContractRegistrationCacheProvider.TryGetForkCache(address, out var caches)) return null;
            var cacheList = caches.ToList();
            if (cacheList.Count == 0) return null;
            var minHeight = cacheList.Min(s => s.BlockHeight);
            var blockHashes = cacheList.Select(s => s.BlockHash).ToList();
            var blockHash = chainContext.BlockHash;
            var blockHeight = chainContext.BlockHeight;
            do
            {
                if (blockHashes.Contains(blockHash)) return cacheList.Last(s => s.BlockHash == blockHash);

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockHash);
                blockHash = link?.PreviousBlockHash;
                blockHeight--;
            } while (blockHash != null && blockHeight >= minHeight);

            return null;
        }

        public bool TryGetSmartContractRegistrationLibCache(Address address, out SmartContractRegistrationCache cache)
        {
            return _smartContractRegistrationCacheProvider.TryGetLibCache(address, out cache);
        }

        public void SetSmartContractRegistrationLibCache(Address address, SmartContractRegistrationCache cache)
        {
            _smartContractRegistrationCacheProvider.SetLibCache(address, cache);
        }

        public async Task<SmartContractCodeHistory> GetSmartContractCodeHistoryAsync(Address address)
        {
            return await _smartContractCodeHistoryService.GetSmartContractCodeHistoryAsync(address);
        }
    }
}