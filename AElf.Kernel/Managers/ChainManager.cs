﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Managers
{
    public class ChainManager : IChainManager
    {
        private readonly IChainStore _chainStore;
        private readonly IDataStore _dataStore;
        private readonly IWorldStateConsole _worldStateConsole;
        
        private IDataProvider _heightOfBlock;

        public ChainManager(IChainStore chainStore, IDataStore dataStore, IWorldStateConsole worldStateConsole)
        {
            _chainStore = chainStore;
            _dataStore = dataStore;
            _worldStateConsole = worldStateConsole;
        }

        public async Task<bool> AppendBlockToChainAsync(IBlock block)
        {
            try
            {
                if(block.Header == null)
                    throw new InvalidDataException("Invalid block");

                var chainId = block.Header.ChainId;
                
                await AppendBlockHeaderAsync(block.Header);

                
            }
            catch (Exception e)
            {
                throw;
            }

            return true;
        }

        public async Task<bool> Exists(Hash chainId)
        {
            var chain = await _chainStore.GetAsync(chainId);
            return chain != null;
        }

        public async Task AppendBlockHeaderAsync(BlockHeader header)
        {
            var chainId = header.ChainId;
            if (await _chainStore.GetAsync(chainId) == null)
                throw new KeyNotFoundException("Not existed Chain");

            var height = await GetChainCurrentHeight(chainId);

            var lastBlockHash = await GetChainLastBlockHash(chainId);

            // chain height should not be 0 when appending a new block
            if (header.Index != height)
            {
                throw new InvalidDataException("Invalid block");
            }
            if (height == 0)
            {
                // empty chain
                lastBlockHash = Hash.Genesis;
            }
            if ( lastBlockHash != header.PreviousBlockHash)
            {
                throw new InvalidDataException("Invalid block");
                //Block is not connected
            }
            
            await InitialHeightOfBlock(chainId);
            await _heightOfBlock.SetAsync(new UInt64Value {Value = header.Index}.CalculateHash(), 
                header.GetHash().ToByteArray());
            
            await SetChainCurrentHeight(chainId, height + 1);
            await SetChainLastBlockHash(chainId, header.GetHash());
        }

        public Task<IChain> GetChainAsync(Hash id)
        {
            return _chainStore.GetAsync(id);
        }

        public Task<IChain> AddChainAsync(Hash chainId, Hash genesisBlockHash)
        {
            return _chainStore.InsertAsync(new Chain(chainId, genesisBlockHash));
        }
        
        /// <inheritdoc/>
        public async Task<ulong> GetChainCurrentHeight(Hash chainId)
        {
            var key = Path.CalculatePointerForCurrentBlockHeight(chainId);
            var heightBytes = await _dataStore.GetDataAsync(key);
            return heightBytes?.ToUInt64() ?? 0;
        }

        /// <inheritdoc/>
        public async Task SetChainCurrentHeight(Hash chainId, ulong height)
        {
            var key = Path.CalculatePointerForCurrentBlockHeight(chainId);
            await _dataStore.SetDataAsync(key, height.ToBytes());
        }

        /// <inheritdoc/>
        public async Task<Hash> GetChainLastBlockHash(Hash chainId)
        {
            var key = Path.CalculatePointerForLastBlockHash(chainId);
            return await _dataStore.GetDataAsync(key);
        }

        /// <inheritdoc/>
        public async Task SetChainLastBlockHash(Hash chainId, Hash blockHash)
        {
            var key = Path.CalculatePointerForLastBlockHash(chainId);
            _worldStateConsole.PreBlockHash = blockHash;
            await _dataStore.SetDataAsync(key, blockHash.GetHashBytes());
        }
        
        private async Task InitialHeightOfBlock(Hash chainId)
        {
            await _worldStateConsole.OfChain(chainId);
            _heightOfBlock = _worldStateConsole.GetAccountDataProvider(Path.CalculatePointerForAccountZero(chainId))
                .GetDataProvider().GetDataProvider("HeightOfBlock");
        }
    }
}