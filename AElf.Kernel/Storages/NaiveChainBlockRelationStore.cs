using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    /// <summary>
    /// Simply use a dictionary to store and get the relations.
    /// </summary>
    public class NaiveChainBlockRelationStore : IChainBlockRelationStore
    {
        private static readonly Dictionary<IHash, List<IHash<IBlock>>> Relations = new Dictionary<IHash, List<IHash<IBlock>>>();

        public Task InsertAsync(IHash<IChain> chainHash, IHash<IBlock> blockHash, long height)
        {
            //Temporary
            Relations[chainHash][(int)height] = blockHash;
            return Task.CompletedTask;
        }

        public Task<IHash<IBlock>> GetAsync(IHash<IChain> chainHash, long height)
        {
            //Temporary
            return Task.FromResult(Relations[chainHash][(int) height]);
        }
    }
}