using System;
using System.Collections.Generic;
using System.IO;
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
        protected readonly IChainManagerBasic _chainManager;
        protected readonly IBlockManagerBasic _blockManager;
        protected readonly ICanonicalHashStore _canonicalHashStore;

        public LightChain(Hash chainId,
            IChainManagerBasic chainManager,
            IBlockManagerBasic blockManager, ICanonicalHashStore canonicalHashStore)
        {
            _chainId = chainId;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _canonicalHashStore = canonicalHashStore;
        }

        public async Task<Hash> GetCurrentBlockHashAsync()
        {
            var hash = await _chainManager.GetCurrentBlockHashAsync(_chainId);
            return hash;
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
            await CheckHeaderAppendable(header);
            await _blockManager.AddBlockHeaderAsync((BlockHeader) header);
            await MaybeSwitchBranch(header);
        }

        protected Hash GetHeightHash(ulong height)
        {
            return ResourcePath.CalculatePointerForGettingBlockHashByHeight(_chainId, height);
        }
        
        protected async Task<Hash> GetCanonicalHashAsync(ulong height)
        {
            var blockHash = await _canonicalHashStore.GetAsync(GetHeightHash(height));
            return blockHash;
        }

        protected async Task CheckHeaderAppendable(IBlockHeader header)
        {
            var prevHeader = await GetHeaderByHashAsync(((BlockHeader)header).PreviousBlockHash);
            if (prevHeader == null)
            {
                throw new InvalidOperationException("Parent is unknown.");
            }
            if(((BlockHeader)header).Index !=((BlockHeader)prevHeader).Index+1)
            {
                throw new InvalidOperationException("Incorrect index.");
            }
        }

        protected async Task<Tuple<List<IBlockHeader>, List<IBlockHeader>>> GetComparedBranchesAsync(IBlockHeader oldHead,
            IBlockHeader newHead)
        {
            var tempOldHead = (BlockHeader)oldHead;
            var tempNewHead = (BlockHeader)newHead;
            var oldBranch = new List<IBlockHeader>();
            var newBranch = new List<IBlockHeader>();
            while (((BlockHeader) oldHead).Index > ((BlockHeader) newHead).Index)
            {
                oldBranch.Add(tempOldHead);
                tempOldHead = (BlockHeader) await GetHeaderByHashAsync(tempOldHead.PreviousBlockHash);
            }
            while (((BlockHeader) newHead).Index > ((BlockHeader) oldHead).Index)
            {
                newBranch.Add(tempNewHead);
                tempNewHead = (BlockHeader) await GetHeaderByHashAsync(tempNewHead.PreviousBlockHash);
            }

            while (tempOldHead.PreviousBlockHash != tempNewHead.PreviousBlockHash)
            {
                oldBranch.Add(tempOldHead);
                newBranch.Add(tempNewHead);
                tempOldHead = (BlockHeader) await GetHeaderByHashAsync(tempOldHead.PreviousBlockHash);
                tempNewHead = (BlockHeader) await GetHeaderByHashAsync(tempNewHead.PreviousBlockHash);                
            }

            if (tempOldHead != null && tempNewHead != null)
            {
                oldBranch.Add(tempOldHead);
                newBranch.Add(tempNewHead);
            }
            return Tuple.Create(oldBranch, newBranch);
        }
        
        protected async Task MaybeSwitchBranch(IBlockHeader header)
        {
            var currentHeader = await GetHeaderByHashAsync(await GetCurrentBlockHashAsync());
            if (currentHeader.GetHash().Equals(((BlockHeader) header).PreviousBlockHash))
            {
                await _canonicalHashStore.InsertOrUpdateAsync(GetHeightHash(((BlockHeader) header).Index), header.GetHash());
                return;
            }
            if (((BlockHeader) header).Index > ((BlockHeader) currentHeader).Index)
            {
                await _chainManager.UpdateCurrentBlockHashAsync(_chainId, header.GetHash());
                var branches = await GetComparedBranchesAsync(currentHeader, header);
                if (branches.Item2.Count > 0)
                {
                    foreach (var newBranchHeader in branches.Item2)
                    {
                        await _canonicalHashStore.InsertOrUpdateAsync(GetHeightHash(((BlockHeader) newBranchHeader).Index), newBranchHeader.GetHash());
                    }
                }
            }
        }
    }
}