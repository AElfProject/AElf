using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Types;

namespace AElf.SmartContract
{
    public interface ITentativeDataProvider : IDataProvider
    {
        IEnumerable<StateValueChange> GetValueChanges();
        Dictionary<Hash, StateCache> StateCache { get; set; } //temporary solution to let data provider access actor's state cache
        void ClearTentativeCache();
    }
}