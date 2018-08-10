using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.ChainController
{
    public interface IContractTxPool : IPool
    {
        /// <summary>
        /// the maximal number of tx in one block
        /// </summary>
        ulong Least { get; }
        
        /// <summary>
        /// the minimal number of tx in one block
        /// </summary>
        ulong Limit { get; }

        void SetBlockVolume(ulong minimal, ulong maximal);
    }
}