using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChainBlockRelationStore
    {
        Task InsertAsync(IHash chainHash, IHash blockHash, long height);

        Task<IHash> GetAsync(IHash chainHash, long height);
    }
    
    /// <summary>
    /// Simply use a dictionary to store and get the relations.
    /// </summary>
    public class ChainBlockRelationStore : IChainBlockRelationStore
    {
        private static readonly Dictionary<IHash, List<IHash>> Relations = 
            new Dictionary<IHash, List<IHash>>();

        public Task InsertAsync(IHash chainHash, IHash blockHash, long height)
        {
            //Temporary
            Relations[chainHash][(int)height] = blockHash;
            return Task.CompletedTask;
        }

        public Task<IHash> GetAsync(IHash chainHash, long height)
        {
            //Temporary
            return Task.FromResult(Relations[chainHash][(int) height]);
        }
    }
}