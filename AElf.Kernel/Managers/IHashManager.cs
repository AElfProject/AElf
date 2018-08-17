using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    //TODO: Finish the map.
    /// <summary>
    /// Act as a map, based on Hash.HashType.
    /// Rules:
    /// ResourcePath -> ResourcePointer
    /// StateHash -> BlockHash
    /// BlockHash -> StateHash
    /// </summary>
    public interface IHashManager
    {
        Task<Hash> GetHash(Hash hash);
        Task SetHash(Hash hash, Hash another);
    }
}