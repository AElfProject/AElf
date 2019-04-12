using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel
{
    public interface IStateCache
    {
        bool TryGetValue(ScopedStatePath key, out byte[] value);
        byte[] this[ScopedStatePath key] { set; }
    }

    /// <summary>
    /// a running chain context
    /// </summary>
    public interface IChainContext
    {
        long BlockHeight { get; set; }
        Hash BlockHash { get; set; }
        IStateCache StateCache { get; set; }
    }
}