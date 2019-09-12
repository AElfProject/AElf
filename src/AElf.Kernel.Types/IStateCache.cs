using AElf.Types;

namespace AElf.Kernel
{
    public interface IStateCache
    {
        bool TryGetValue(ScopedStatePath key, out byte[] value);
        byte[] this[ScopedStatePath key] { set; }
    }
}