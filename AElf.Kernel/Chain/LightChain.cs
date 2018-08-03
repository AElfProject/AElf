using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Akka.Dispatch;

namespace AElf.Kernel
{
    public class LightChain : ILightChain
    {
        protected readonly Hash _chainId;
        protected readonly IBlockManagerBasic _blockManager;
        protected readonly ICanonicalHashStore _canonicalHashStore;

        public LightChain(Hash chainId, IBlockManagerBasic blockManager, ICanonicalHashStore canonicalHashStore)
        {
            _chainId = chainId;
            _blockManager = blockManager;
            _canonicalHashStore = canonicalHashStore;
        }

        public async Task<bool> HasHeader(Hash blockId)
        {
            var header = await _blockManager.GetBlockHeaderAsync(blockId);
            return header != null;
        }
        
        public async Task AddHeadersAsync(IEnumerable<IBlockHeader> headers)
        {
            foreach (var header in headers)
            {
                await AddHeaderAsync(header);
            }
        }

        public async Task<IBlockHeader> GetHeaderByHashAsync(Hash blockId)
        {
            return await _blockManager.GetBlockHeaderAsync(blockId);
        }

        public async Task<IBlockHeader> GetHeaderByHeightAsync(ulong height)
        {
            var blockHash = await GetCanonicalHashAsync(height);
            if (blockHash == null)
            {
                return null;
            }

            return await GetHeaderByHashAsync(blockHash);
        }

        public async Task<bool> IsOnCanonical(Hash blockId)
        {
            var header = (BlockHeader) await GetHeaderByHashAsync(blockId);
            if (header == null)
            {
                return false;
            }

            var canonicalHash = await GetCanonicalHashAsync(header.Index);
            return canonicalHash == blockId;
        }
        
        protected async Task AddHeaderAsync(IBlockHeader header)
        {
            await _blockManager.AddBlockHeaderAsync((BlockHeader) header);
        }

        protected async Task<Hash> GetCanonicalHashAsync(ulong height)
        {
            var heightHash = ResourcePath.CalculatePointerForGettingBlockHashByHeight(_chainId, height);
            var blockHash = await _canonicalHashStore.GetAsync(heightHash);
            return blockHash;
        }
    }
}