using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Database
{
    public class KeyValueDatabase : IKeyValueDatabase
    {
        private readonly ConcurrentDictionary<string, byte[]> _dictionary = new ConcurrentDictionary<string, byte[]>();
        
        public Task<byte[]> GetAsync(string key, Type type)
        {
            return _dictionary.TryGetValue(key, out var value) ? Task.FromResult(value) : Task.FromResult<byte[]>(null);
        }

        public Task SetAsync(string key, ISerializable data)
        {
            _dictionary[key] = data.Serialize();
            return Task.CompletedTask;
        }

        public bool IsConnected()
        {
            return true;
        }
    }
}