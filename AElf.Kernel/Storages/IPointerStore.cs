using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IPointerStore
    {
        Task Insert(IHash<IPath> path, IHash<IPath> pointer);

        Task<IHash<IPath>> GetAsync(IHash<IPath> path);
    }
    
    public class PointerStore : IPointerStore
    {
        private static readonly Dictionary<IHash<IPath>, IHash<IPath>> Blocks = new Dictionary<IHash<IPath>, IHash<IPath>>();

        public Task Insert(IHash<IPath> path, IHash<IPath> pointer)
        {
            Blocks[path] = pointer;
            return Task.CompletedTask;
        }

        public Task<IHash<IPath>> GetAsync(IHash<IPath> path)
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