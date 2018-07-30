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
                return (await _keyValueDatabase.GetAsync(key)).Deserialize<T>();
            }
            catch (Exception e)
            {    
                Console.WriteLine(e);
                return default(T);
            }
        }

        public async Task<bool> PipelineSetDataAsync<T>(Dictionary<Hash, byte[]> pipelineSet) where T : IMessage
        {
            try
            {
                if(!typeof(T).GetInterfaces().Contains(typeof(IMessage)))
                {
                    throw new Exception("Wrong Data Type");
                }
                if (!Enum.TryParse<Types>(typeof(T).Name, out var result))
                {
                    throw new Exception($"Not Supported Data Type {typeof(T).Name}");
                }
                return await _keyValueDatabase.PipelineSetAsync(
                    pipelineSet.ToDictionary(kv => kv.Key.GetKeyString((uint)result), kv => kv.Value));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}