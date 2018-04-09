using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IWorldStateStore
    {
        /// <summary>
        /// Store current world state.
        /// </summary>
        /// <returns></returns>
        Task SetWorldStateAsync(IHash chainHash, IChangesStore changesStore);

        /// <summary>
        /// Get the world state by corresponding block height of corresponding chain.
        /// </summary>
        /// <param name="chainHash"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        Task<WorldState> GetAsync(IHash chainHash, long height);

        /// <summary>
        /// Get latest world state of corresponding chain.
        /// </summary>
        /// <returns></returns>
        Task<WorldState> GetAsync(IHash chainHash);
    }
}