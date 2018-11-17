using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class StateStore : IStateStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        
        private const string _dbName = "State";

        public StateStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        private static string GetKey(StatePath path)
        {
            return $"{GlobalConfig.StatePrefix}{path.GetHash().DumpHex()}";
        }

        public async Task SetAsync(StatePath path, byte[] value)
        {
            try
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var key = GetKey(path);
                await _keyValueDatabase.SetAsync(_dbName,key, value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<byte[]> GetAsync(StatePath path)
        {
            try
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                var key = GetKey(path);
                var res = await _keyValueDatabase.GetAsync(_dbName,key);
//                return res ?? new byte[0];
                return res;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<bool> PipelineSetDataAsync(Dictionary<StatePath, byte[]> pipelineSet)
        {
            try
            {
                var dict = pipelineSet.ToDictionary(kv => GetKey(kv.Key), kv => kv.Value);
                return await _keyValueDatabase.PipelineSetAsync(_dbName,dict);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}