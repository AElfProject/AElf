using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// World State presents the state of a chain, changed by block. 
    /// </summary>
    public interface IWorldState
    {
        /// <summary>
        /// The merkle tree root presents the world state of a chain
        /// </summary>
        /// <returns></returns>
        Task<Hash> GetWorldStateMerkleTreeRootAsync();

        Task<Change> GetChangeAsync(Hash pathHash);
    }
}