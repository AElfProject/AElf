using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Domain
{
    public interface IBlockchainStateManager
    {
        //Task<VersionedState> GetVersionedStateAsync(Hash blockHash,long blockHeight, string key);
        Task<ByteString> GetStateAsync(string key, long blockHeight, Hash blockHash);
    }

    public class BlockchainStateManager : BlockchainStateBaseManager, IBlockchainStateManager
    {
        public BlockchainStateManager(IStateStore<VersionedState> versionedStates,
            INotModifiedCachedStateStore<BlockStateSet> blockStateSets) : base(versionedStates, blockStateSets)
        {
        }

        protected override bool TryGetFromBlockStateSet(BlockStateSet blockStateSet, string key, out ByteString value)
        {
            value = null;
            return blockStateSet != null && blockStateSet.TryGetState(key, out value);
        }

        public async Task<ByteString> GetStateAsync(string key, long blockHeight, Hash blockHash)
        {
            return (await GetAsync(key, blockHeight, blockHash)).Value;
        }
    }


    public interface IBlockStateSetManger
    {
        Task SetBlockStateSetAsync(BlockStateSet blockStateSet);
        Task MergeBlockStateAsync(ChainStateInfo chainStateInfo, Hash blockStateHash);
        Task<ChainStateInfo> GetChainStateInfoAsync();
        Task<BlockStateSet> GetBlockStateSetAsync(Hash blockHash);
        Task RemoveBlockStateSetsAsync(IList<Hash> blockStateHashes);
    }


    public interface IBlockchainExecutedDataManager
    {
        Task<StateReturn> GetExecutedCacheAsync(string key, long blockHeight, Hash blockHash);

        Task AddBlockExecutedCacheAsync(Hash blockHash, IDictionary<string, ByteString> blockExecutedCache);
    }


    public class BlockchainExecutedDataManager : BlockchainStateBaseManager, IBlockchainExecutedDataManager,
        ITransientDependency
    {
        private IBlockStateSetManger _blockStateSetManger;

        public BlockchainExecutedDataManager(IStateStore<VersionedState> versionedStates,
            INotModifiedCachedStateStore<BlockStateSet> blockStateSets, IBlockStateSetManger blockStateSetManger) :
            base(versionedStates, blockStateSets)
        {
            _blockStateSetManger = blockStateSetManger;
        }

        protected override bool TryGetFromBlockStateSet(BlockStateSet blockStateSet, string key, out ByteString value)
        {
            value = null;
            return blockStateSet != null && blockStateSet.TryGetExecutedCache(key, out value);
        }

        public async Task<StateReturn> GetExecutedCacheAsync(string key, long blockHeight, Hash blockHash)
        {
            return (await GetAsync(key, blockHeight, blockHash));
        }

        public async Task AddBlockExecutedCacheAsync(Hash blockHash, IDictionary<string, ByteString> blockExecutedCache)
        {
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(blockHash);
            if (blockStateSet == null) return;
            foreach (var keyPair in blockExecutedCache)
            {
                blockStateSet.BlockExecutedData[keyPair.Key] = keyPair.Value;
            }

            await _blockStateSets.SetWithCacheAsync(blockStateSet.BlockHash.ToStorageKey(), blockStateSet);
        }
    }

    public class StateReturn
    {
        public ByteString Value { get; set; }
        public bool IsInStore { get; set; }
    }

    public abstract class BlockchainStateBaseManager
    {
        protected readonly IStateStore<VersionedState> _versionedStates;
        protected readonly INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;

        public BlockchainStateBaseManager(IStateStore<VersionedState> versionedStates,
            INotModifiedCachedStateStore<BlockStateSet> blockStateSets)
        {
            _versionedStates = versionedStates;
            _blockStateSets = blockStateSets;
        }

        protected abstract bool
            TryGetFromBlockStateSet(BlockStateSet blockStateSet, string key, out ByteString value);

        protected async Task<StateReturn> GetAsync(string key, long blockHeight, Hash blockHash)
        {
            ByteString value = null;
            var isInStore = false;
            //first DB read
            var bestChainState = await _versionedStates.GetAsync(key);

            if (bestChainState != null)
            {
                if (bestChainState.BlockHash == blockHash)
                {
                    value = bestChainState.Value;
                    isInStore = true;
                }
                else
                {
                    if (bestChainState.BlockHeight >= blockHeight)
                    {
                        //because we may clear history state
                        throw new InvalidOperationException(
                            $"cannot read history state, best chain state hash: {bestChainState.BlockHash.ToHex()}, key: {key}, block height: {blockHeight}, block hash{blockHash.ToHex()}");
                    }

                    //find value in block state set
                    var blockStateSet = await FindBlockStateSetWithKeyAsync(key, bestChainState.BlockHeight, blockHash);
                    
                    TryGetFromBlockStateSet(blockStateSet, key, out value);

                    if (value == null && (blockStateSet == null || !blockStateSet.Deletes.Contains(key) ||
                                          blockStateSet.BlockHeight <= bestChainState.BlockHeight))
                    {
                        //not found value in block state sets. for example, best chain is 100, blockHeight is 105,
                        //it will find 105 ~ 101 block state set. so the value could only be the best chain state value.
                        // retry versioned state in case conflict of get state during merging  
                        bestChainState = await _versionedStates.GetAsync(key);
                        value = bestChainState.Value;
                        isInStore = true;
                    }
                }
            }
            else
            {
                //best chain state is null, it will find value in block state set
                var blockStateSet = await FindBlockStateSetWithKeyAsync(key, 0, blockHash);

                TryGetFromBlockStateSet(blockStateSet, key, out value);

                if (value == null && blockStateSet == null)
                {
                    // retry versioned state in case conflict of get state during merging  
                    bestChainState = await _versionedStates.GetAsync(key);
                    value = bestChainState?.Value;
                }
            }


            return new StateReturn()
            {
                Value = value,
                IsInStore = isInStore
            };
        }

        private async Task<BlockStateSet> FindBlockStateSetWithKeyAsync(string key, long bestChainHeight,
            Hash blockHash)
        {
            var blockStateKey = blockHash.ToStorageKey();
            var blockStateSet = await _blockStateSets.GetAsync(blockStateKey);

            while (blockStateSet != null && blockStateSet.BlockHeight > bestChainHeight)
            {
                if (
                    TryGetFromBlockStateSet(blockStateSet, key, out _)) break;

                blockStateKey = blockStateSet.PreviousHash?.ToStorageKey();

                if (blockStateKey != null)
                {
                    blockStateSet = await _blockStateSets.GetAsync(blockStateKey);
                }
                else
                {
                    blockStateSet = null;
                }
            }

            return blockStateSet;
        }
    }

    public class BlockStateSetManger : IBlockStateSetManger, ITransientDependency
    {
        protected readonly IStateStore<VersionedState> _versionedStates;
        protected readonly INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;

        private readonly IStateStore<ChainStateInfo> _chainStateInfoCollection;
        private readonly int _chainId;


        public BlockStateSetManger(IStateStore<VersionedState> versionedStates,
            INotModifiedCachedStateStore<BlockStateSet> blockStateSets,
            IStateStore<ChainStateInfo> chainStateInfoCollection, IOptionsSnapshot<ChainOptions> options)
        {
            _versionedStates = versionedStates;
            _blockStateSets = blockStateSets;
            _chainStateInfoCollection = chainStateInfoCollection;
            _chainId = options.Value.ChainId;
        }


        public async Task MergeBlockStateAsync(ChainStateInfo chainStateInfo, Hash blockStateHash)
        {
            var blockState = await _blockStateSets.GetAsync(blockStateHash.ToStorageKey());
            if (blockState == null)
            {
                if (chainStateInfo.Status == ChainStateMergingStatus.Merged &&
                    chainStateInfo.MergingBlockHash == blockStateHash)
                {
                    chainStateInfo.Status = ChainStateMergingStatus.Common;
                    chainStateInfo.MergingBlockHash = null;

                    await _chainStateInfoCollection.SetAsync(chainStateInfo.ChainId.ToStorageKey(), chainStateInfo);
                    return;
                }

                throw new InvalidOperationException($"cannot get block state of {blockStateHash}");
            }

            if (chainStateInfo.BlockHash == null || chainStateInfo.BlockHash == blockState.PreviousHash ||
                (chainStateInfo.Status == ChainStateMergingStatus.Merged &&
                 chainStateInfo.MergingBlockHash == blockState.BlockHash))
            {
                chainStateInfo.Status = ChainStateMergingStatus.Merging;
                chainStateInfo.MergingBlockHash = blockStateHash;

                await _chainStateInfoCollection.SetAsync(chainStateInfo.ChainId.ToStorageKey(), chainStateInfo);
                var dic = blockState.Changes.Concat(blockState.BlockExecutedData).Select(change => new VersionedState
                {
                    Key = change.Key,
                    Value = change.Value,
                    BlockHash = blockState.BlockHash,
                    BlockHeight = blockState.BlockHeight,
                    //OriginBlockHash = origin.BlockHash
                }).ToDictionary(p => p.Key, p => p);

                await _versionedStates.SetAllAsync(dic);

                await _versionedStates.RemoveAllAsync(blockState.Deletes.ToList());

                chainStateInfo.Status = ChainStateMergingStatus.Merged;
                chainStateInfo.BlockHash = blockState.BlockHash;
                chainStateInfo.BlockHeight = blockState.BlockHeight;
                await _chainStateInfoCollection.SetAsync(chainStateInfo.ChainId.ToStorageKey(), chainStateInfo);

                await _blockStateSets.RemoveAsync(blockStateHash.ToStorageKey());

                chainStateInfo.Status = ChainStateMergingStatus.Common;
                chainStateInfo.MergingBlockHash = null;

                await _chainStateInfoCollection.SetAsync(chainStateInfo.ChainId.ToStorageKey(), chainStateInfo);
            }
            else
            {
                throw new InvalidOperationException(
                    "cannot merge block not linked, check new block's previous block hash ");
            }
        }

        public async Task SetBlockStateSetAsync(BlockStateSet blockStateSet)
        {
            await _blockStateSets.SetAsync(GetKey(blockStateSet), blockStateSet);
        }

        protected string GetKey(BlockStateSet blockStateSet)
        {
            return blockStateSet.BlockHash.ToStorageKey();
        }


        public async Task<BlockStateSet> GetBlockStateSetAsync(Hash blockHash)
        {
            return await _blockStateSets.GetAsync(blockHash.ToStorageKey());
        }

        public async Task RemoveBlockStateSetsAsync(IList<Hash> blockStateHashes)
        {
            await _blockStateSets.RemoveAllAsync(blockStateHashes.Select(b => b.ToStorageKey()).ToList());
        }

        public async Task<ChainStateInfo> GetChainStateInfoAsync()
        {
            var chainStateInfo = await _chainStateInfoCollection.GetAsync(_chainId.ToStorageKey());
            return chainStateInfo ?? new ChainStateInfo {ChainId = _chainId};
        }
    }
}