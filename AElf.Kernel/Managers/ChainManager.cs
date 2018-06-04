using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Managers
{
    public class ChainManager : IChainManager
    {
        private readonly IChainStore _chainStore;
        private readonly IDataStore _dataStore;

        public ChainManager(IChainStore chainStore, IDataStore dataStore)
        {
            _chainStore = chainStore;
            _dataStore = dataStore;
        }

        public async Task AppendBlockToChainAsync(Hash chainId, IBlock block)
        {
            if(block.Header == null)
                throw new InvalidDataException("Invalid block");

            await AppednBlockHeaderAsync(chainId, block.Header);
        }

        public async Task AppednBlockHeaderAsync(Hash chainId, BlockHeader header)
        {
            if (await _chainStore.GetAsync(chainId) == null)
                throw new KeyNotFoundException("Not existed Chain");
            
            var height = await GetChainCurrentHeight(chainId);
            var lastBlockHash = await GetChainLastBlockHash(chainId);
            // chain height should not be 0 when appending a new block
            if (height == 0)
            {
                // empty chain
                await SetChainCurrentHeight(chainId, 1);
                await SetChainLastBlockHash(chainId, header.GetHash());
            }
            else if ( lastBlockHash != header.PreviousHash)
            {
                throw new InvalidDataException("Invalid block");
                //Block is not connected
            }
            header.Index = height;
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
            return heightBytes == null ? 0 : UInt64.Parser.ParseFrom(heightBytes.Value).Value;
        }

        /// <inheritdoc/>
        public async Task SetChainCurrentHeight(Hash chainId, ulong height)
        {
            var key = Path.CalculatePointerForCurrentBlockHeight(chainId);
            await _dataStore.SetDataAsync(key,
                new Data {Value = ByteString.CopyFrom(new UInt64 {Value = height}.ToByteArray())});
        }

        /// <inheritdoc/>
        public async Task<Hash> GetChainLastBlockHash(Hash chainId)
        {
            var key = Path.CalculatePointerForLastBlockHash(chainId);
            var data = await _dataStore.GetDataAsync(key);
            return data == null ? Hash.Zero : new Hash(data.Value);
        }

        /// <inheritdoc/>
        public async Task SetChainLastBlockHash(Hash chainId, Hash blockHash)
        {
            var key = Path.CalculatePointerForLastBlockHash(chainId);
            await _dataStore.SetDataAsync(key, new Data {Value = blockHash.Value});
        }
        
    }
}