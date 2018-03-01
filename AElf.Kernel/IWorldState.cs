﻿using AElf.Kernel.Merkle;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// World State presents the state of a chain, changed by block. 
    /// </summary>
    public interface IWorldState
    {
        /// <summary>
        /// Get a data provider from the accounts address
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        //IAccountDataProvider GetAccountDataProviderByAccount(IAccount account);
        
        /// <summary>
        /// The merkle tree root presents the world state of a chain
        /// </summary>
        /// <returns></returns>
        Task<IHash<IMerkleTree<IHash>>> GetWorldStateMerkleTreeRootAsync();
    }
    
    
}