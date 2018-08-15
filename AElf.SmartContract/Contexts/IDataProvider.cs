using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.SmartContract
{
    /// <summary>
    /// A DataProvider is used to access database and will cause changes.
    /// </summary>
    public interface IDataProvider
    {
        IDataProvider GetDataProvider(string name);

        Task SetAsync(Hash keyHash, byte[] obj);

        Task<byte[]> GetAsync(Hash keyHash);
        
        Task<byte[]> GetAsync(Hash keyHash, Hash preBlockHash);

        Hash GetPathFor(Hash keyHash);

        Hash GetHash();
    }
}