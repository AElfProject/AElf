using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public interface ICachedDataProvider : IDataProvider
    {
        IEnumerable<StateValueChange> GetValueChanges();
        void ClearCache();
    }
}