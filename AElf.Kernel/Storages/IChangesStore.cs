using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChangesStore
    {
        Task Insert(IHash path, IHash before, IHash after);
    }
    
    public class ChangesStore : IChangesStore
    {
        private static readonly Dictionary<IHash, List<IHash>> Changes = 
            new Dictionary<IHash, List<IHash>>();

        public Task Insert(IHash path, IHash before, IHash after)
        {
            Changes[path][0] = before;
            Changes[path][1] = after;
            return Task.CompletedTask;
        }
    }
}