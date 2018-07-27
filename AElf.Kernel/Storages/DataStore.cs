using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using System.Linq;
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
               
        public async Task SetDataAsync<T>(Hash pointerHash, byte[] data) where T : IMessage
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
                
                var key = pointerHash.GetKeyString((uint)result);
                await _keyValueDatabase.SetAsync(key, data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task<byte[]> GetDataAsync<T>(Hash pointerHash) where T : IMessage
        {
            try
            {
                if(!typeof(T).GetInterfaces().Contains(typeof(IMessage)))
                {
                    throw new Exception("Wrong Data Type");
                }
                if (pointerHash == null)
                {
                    return null;
                }
                if (!Enum.TryParse<Types>(typeof(T).Name, out var result))
                {
                    throw new Exception($"Not Supported Data Type {typeof(T).Name}");
                }
                
                var key = pointerHash.GetKeyString((uint)result);
                return await _keyValueDatabase.GetAsync(key, typeof(byte[]));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
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