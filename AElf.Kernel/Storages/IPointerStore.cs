using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IPointerStore
    {
        Task Insert(IHash path, IHash pointer);

        Task<IHash> GetAsync(IHash path);
    }
    
    public class PointerStore : IPointerStore
    {
        private static readonly Dictionary<IHash, IHash> Blocks = new Dictionary<IHash, IHash>();

        public Task Insert(IHash path, IHash pointer)
        {
            Blocks[path] = pointer;
            return Task.CompletedTask;
        }

        public Task<IHash> GetAsync(IHash path)
        {
            foreach (var k in Blocks.Keys)
            {
                if (k.Equals(path))
                {
                    return Task.FromResult(Blocks[k]);
                }
            }
            throw new InvalidOperationException("Cannot find corresponding pointer.");
        }
    }
}