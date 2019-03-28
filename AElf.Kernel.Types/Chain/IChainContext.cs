using AElf.Common;

namespace AElf.Kernel
{
    public interface IStateCache
    {
        byte[] this[StatePath key] { set; }
        bool TryGetValue(StatePath key, out byte[] value);
    }

    /// <summary>
    ///     a running chain context
    /// </summary>
    public interface IChainContext
    {
        long BlockHeight { get; set; }
        Hash BlockHash { get; set; }
        IStateCache StateCache { get; set; }
    }
}