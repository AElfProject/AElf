using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel
{
    public interface IStateCache
    {
        bool TryGetValue(StatePath key, out byte[] value);
        byte[] this[StatePath key] { set; }
    }

    /// <summary>
    /// a running chain context
    /// </summary>
    public interface IChainContext
    {
        ulong BlockHeight { get; set; }
        Hash BlockHash { get; set; }
        IStateCache StateCache { get; set; }
    }
}