using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public interface IBlockStore
    {
        Task Insert(Block block);

        Task<Block> GetAsync(Hash blockHash);
    }
    
    public class BlockStore : IBlockStore
    {
        private IKeyValueDatabase _keyValueDatabase;
        
        private readonly Dictionary<IHash, Block> _blocks = new Dictionary<IHash, Block>();

        public BlockStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task Insert(Block block)
        {
            await _keyValueDatabase.SetAsync(block);
        }

        public async Task<Block> GetAsync(Hash blockHash)
        {
            return (Block) await  _keyValueDatabase.GetAsync(blockHash);
        }
    }
    
}