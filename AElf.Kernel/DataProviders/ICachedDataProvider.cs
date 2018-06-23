using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Kernel
{
    public interface ICachedDataProvider : IDataProvider
    {
        IEnumerable<StateValueChange> GetValueChanges();
        void ClearCache();
    }
}