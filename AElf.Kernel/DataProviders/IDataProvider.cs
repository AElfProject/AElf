using System.Threading.Tasks;
using AElf.Database;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public interface IDataProvider
    {
        IDataProvider GetDataProvider(string name);

        Task<Change> SetAsync(Hash keyHash, ISerializable obj);

        Task<Data> GetAsync(Hash keyHash);
        
        Task<Data> GetAsync(Hash keyHash, Hash preBlockHash);

        Hash GetHash();
    }
}