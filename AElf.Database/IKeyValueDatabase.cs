using System;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Database
{
    public interface IKeyValueDatabase
    {
        Task<byte[]> GetAsync(string key,Type type);
        Task SetAsync(string key, ISerializable bytes);
        bool IsConnected();
    }
}