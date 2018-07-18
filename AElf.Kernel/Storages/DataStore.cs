﻿using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using System.Linq;

namespace AElf.Kernel.Storages
{
    public class DataStore : IDataStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public DataStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task SetDataAsync(Hash pointerHash, byte[] data)
        {
            await _keyValueDatabase.SetAsync(pointerHash.ToHex(), data);
        }

        public async Task<byte[]> GetDataAsync(Hash pointerHash)
        {
            if (pointerHash == null)
            {
                return null;
            }
            return await _keyValueDatabase.GetAsync(pointerHash.ToHex(), typeof(byte[]));
        }

        public async Task<bool> PipelineSetDataAsync(Dictionary<Hash, byte[]> pipelineSet)
        {
            return await _keyValueDatabase.PipelineSetAsync(pipelineSet.ToDictionary(kv=> kv.Key.ToHex(), kv=>kv.Value));
        }
    }
}