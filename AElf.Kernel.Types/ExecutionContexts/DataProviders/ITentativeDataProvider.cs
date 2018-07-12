using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public interface ITentativeDataProvider : IDataProvider
    {
        IEnumerable<StateValueChange> GetValueChanges();
        Dictionary<Hash, byte[]> StateCache { get; set; } //temporary solution to let data provider access actor's state cache
        void ClearCache();
    }
}