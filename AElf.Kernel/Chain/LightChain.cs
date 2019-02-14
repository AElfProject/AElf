using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class LightChain : ILightChain
    {
        protected readonly int _chainId;
        protected readonly IChainManager _chainManager;
        protected readonly IBlockManager _blockManager;

        public ILogger<LightChain> Logger {get;set;}
        public LightChain(int chainId,
            IChainManager chainManager,
            IBlockManager blockManager)
        {
            _chainId = chainId;
            _chainManager = chainManager;
            _blockManager = blockManager;
            Logger = NullLogger<LightChain>.Instance;
        }

        public async Task<ulong> GetCurrentBlockHeightAsync()
        {
            var hash = await _chainManager.GetCurrentBlockHashAsync(_chainId);
            if (hash.IsNull())
            {
                return GlobalConfig.GenesisBlockHeight;
            }

            var header = (BlockHeader) await GetHeaderByHashAsync(hash);
            return header.Height;
        }

        public async Task<Hash> GetCurrentBlockHashAsync()
        {
            var hash = await _chainManager.GetCurrentBlockHashAsync(_chainId);
            return hash;
        }

        public async Task<bool> HasHeader(Hash blockHash)
        {
            var header = await _blockManager.GetBlockHeaderAsync(blockHash);
            return header != null;
        }

        public async Task AddHeadersAsync(IEnumerable<IBlockHeader> headers)
        {
            foreach (var header in headers)
            {
                await AddHeaderAsync(header);
            }
        }

        public async Task<IBlockHeader> GetHeaderByHashAsync(Hash blockHash)
        {
            return await _blockManager.GetBlockHeaderAsync(blockHash);
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

            var canonicalHash = await GetCanonicalHashAsync(header.Height);
            return canonicalHash == blockId;
        }

        protected async Task AddHeaderAsync(IBlockHeader header)
        {
            await CheckHeaderAppendable(header);
            await _blockManager.AddBlockHeaderAsync((BlockHeader) header);
            await MaybeSwitchBranch(header);
            MessageHub.Instance.Publish((BlockHeader) header);
        }

        public async Task<Hash> GetCanonicalHashAsync(ulong height)
        {
            var blockHash = await _chainManager.GetCanonical(_chainId, height);
            return blockHash;
        }

        protected async Task CheckHeaderAppendable(IBlockHeader header)
        {
            var blockHeader = (BlockHeader) header;

            #region genesis

            // TODO: more strict genesis
            if (blockHeader.Height == GlobalConfig.GenesisBlockHeight)
            {
                var curHash = await _chainManager.GetCurrentBlockHashAsync(_chainId);
                if (curHash.IsNull())
                {
                    await _chainManager.AddChainAsync(_chainId, header.GetHash());
                }

                return;
            }

            #endregion genesis

            var prevHeader = await GetHeaderByHashAsync(blockHeader.PreviousBlockHash);
            if (prevHeader == null)
            {
                throw new InvalidOperationException($"Parent is unknown for {blockHeader}.");
            }

            var expected = ((BlockHeader) prevHeader).Height + 1;
            var actual = blockHeader.Height;

            if (actual != expected)
            {
                throw new InvalidOperationException($"Incorrect index. Expected: {expected}, actual: {actual}");
            }
        }

        protected async Task<Tuple<List<IBlockHeader>, List<IBlockHeader>>> GetComparedBranchesAsync(
            IBlockHeader oldHead,
            IBlockHeader newHead)
        {
            var tempOldHead = (BlockHeader) oldHead;
            var tempNewHead = (BlockHeader) newHead;
            var oldBranch = new List<IBlockHeader>();
            var newBranch = new List<IBlockHeader>();
            while (((BlockHeader) oldHead).Height > ((BlockHeader) newHead).Height)
            {
                oldBranch.Add(tempOldHead);
                tempOldHead = (BlockHeader) await GetHeaderByHashAsync(tempOldHead.PreviousBlockHash);
            }

            while (((BlockHeader) newHead).Height > ((BlockHeader) oldHead).Height)
            {
                newBranch.Add(tempNewHead);
                if (tempNewHead == null)
                {
                    break;
                }

                tempNewHead = (BlockHeader) await GetHeaderByHashAsync(tempNewHead.PreviousBlockHash);
            }

            while (tempNewHead != null && tempOldHead.PreviousBlockHash != tempNewHead.PreviousBlockHash)
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
            var blockHeader = (BlockHeader) header;
            if (blockHeader.Height <= GlobalConfig.GenesisBlockHeight)
            {
                await _chainManager.SetCanonical(_chainId, blockHeader.Height, header.GetHash());
                await _chainManager.UpdateCurrentBlockHashAsync(_chainId, header.GetHash());
                return;
            }

            var currentBlockHash = await GetCurrentBlockHashAsync();
            var currentHeader = await GetHeaderByHashAsync(currentBlockHash);
            if (currentHeader.GetHash().Equals(((BlockHeader) header).PreviousBlockHash) ||
                ((BlockHeader) header).PreviousBlockHash.Equals(Hash.Genesis))
            {
                await _chainManager.SetCanonical(_chainId, header.Height, header.GetHash());
                await _chainManager.UpdateCurrentBlockHashAsync(_chainId, header.GetHash());
                return;
            }

            if (((BlockHeader) header).Height > ((BlockHeader) currentHeader).Height)
            {
                await _chainManager.UpdateCurrentBlockHashAsync(_chainId, header.GetHash());
                var branches = await GetComparedBranchesAsync(currentHeader, header);
                if (branches.Item2.Count > 0)
                {
                    foreach (var newBranchHeader in branches.Item2)
                    {
                        if (newBranchHeader == null)
                        {
                            break;
                        }

                        await _chainManager.SetCanonical(_chainId, newBranchHeader.Height, newBranchHeader.GetHash());
                    }
                }
            }
        }
    }
}