using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using System.Linq;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class DataStore : IDataStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public DataStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash pointerHash, byte[] obj)
        {
            if (pointerHash == null)
                return;
            await _keyValueDatabase.SetAsync(pointerHash.ToHex(), obj);
        }

        public async Task<byte[]> GetAsync(Hash pointerHash)
        {
            if (pointerHash == null)
                return null;
            return await _keyValueDatabase.GetAsync(pointerHash.ToHex());
        }

        public async Task InsertAsync<T>(Hash pointerHash, T obj) where T : IMessage
        {
            try
            {
                if (pointerHash == null)
                {
                    throw new Exception("Point hash cannot be null.");
                }
                if (!Enum.TryParse<Types>(typeof(T).Name, out var result))
                {
                    throw new Exception($"Not Supported Data Type, {typeof(T).Name}.");
                }
                
                var key = pointerHash.GetKeyString((uint)result);
                await _keyValueDatabase.SetAsync(key, obj.ToByteArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<T> GetAsync<T>(Hash pointerHash) where T : IMessage, new()
        {
            try
            {
                if (pointerHash == null)
                {
                    throw new Exception("Pointer hash cannot be null.");
                }
                if (!Enum.TryParse<Types>(typeof(T).Name, out var result))
                {
                    throw new Exception($"Not Supported Data Type, {typeof(T).Name}.");
                }
                
                var key = pointerHash.GetKeyString((uint)result);
                var res = await _keyValueDatabase.GetAsync(key);
                return  res == null ? default(T): res.Deserialize<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<bool> PipelineSetDataAsync(Dictionary<Hash, byte[]> pipelineSet)
        {
            try
            {
                return await _keyValueDatabase.PipelineSetAsync(
                    pipelineSet.ToDictionary(kv => kv.Key.ToHex(), kv => kv.Value));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task RemoveAsync<T>(Hash pointerHash) where T : IMessage
        {
            try
            {
                if (pointerHash == null)
                {
                    throw new Exception("Pointer hash cannot be null.");
                }
                if (!Enum.TryParse<Types>(typeof(T).Name, out var result))
                {
                    throw new Exception($"Not Supported Data Type, {typeof(T).Name}.");
                }
                var key = pointerHash.GetKeyString((uint)result);
                await _keyValueDatabase.RemoveAsync(key);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}