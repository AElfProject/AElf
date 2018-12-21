using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Storage.Interfaces;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Storage.Storages
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class DataStore : IDataStore
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

                if (obj == null)
                {
                    throw new Exception("Cannot insert null value.");
                }

                var typeName = typeof(T).Name;
                var key = GetKeyString(pointerHash,typeName);
                await _keyValueDatabase.SetAsync(typeName,key, obj.ToByteArray());
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
                
                var typeName = typeof(T).Name;
                var key = GetKeyString(pointerHash,typeName);
                var res = await _keyValueDatabase.GetAsync(typeName,key);
                return  res == null ? default(T): res.Deserialize<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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

                var typeName = typeof(T).Name;
                var key = GetKeyString(pointerHash,typeName);
                await _keyValueDatabase.RemoveAsync(typeName,key);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        private string GetKeyString(Hash hash, string type)
        {
            return new Key
            {
                Type = type,
                Value = ByteString.CopyFrom(hash.DumpByteArray()),
                HashType = (uint) hash.HashType
            }.ToByteArray().ToHex();
        }
    }
}