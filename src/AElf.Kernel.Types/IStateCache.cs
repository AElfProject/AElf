using AElf.Types;

namespace AElf.Kernel;

public interface IStateCache
{
    byte[] this[ScopedStatePath key] { set; }
    bool TryGetValue(ScopedStatePath key, out byte[] value);
}