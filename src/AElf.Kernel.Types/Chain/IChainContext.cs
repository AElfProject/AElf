namespace AElf.Kernel
{
    // TODO: check, move to other project
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