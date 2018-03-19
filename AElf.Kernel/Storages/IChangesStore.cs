using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChangesStore
    {
        Task Insert(IHash<IPath> path, IHash<IPath> before, IHash<IPath> after);
    }
    
    public class ChangesStore : IChangesStore
    {
        private static readonly Dictionary<IHash<IPath>, List<IHash<IPath>>> Changes = 
            new Dictionary<IHash<IPath>, List<IHash<IPath>>>();

        public Task Insert(IHash<IPath> path, IHash<IPath> before, IHash<IPath> after)
        {
            Changes[path][0] = before;
            Changes[path][1] = after;
            return Task.CompletedTask;
        }
    }
}