using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Types;

namespace AElf.SmartContract
{
    public interface ITentativeDataProvider : IDataProvider
    {
        IEnumerable<StateValueChange> GetValueChanges();
        void ClearTentativeCache();
    }
}