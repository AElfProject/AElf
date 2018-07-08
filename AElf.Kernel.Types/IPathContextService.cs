using AElf.Kernel.Types;

namespace AElf.Kernel
{
    /// <summary>
    /// There exists a Pointer class in System.Reflection namespace,
    /// so use "Path" to avoid the misleading.
    /// </summary>
    public interface IPathContextService
    {
        PathContextService SetChainHash(Hash chainHash);
        PathContextService SetBlockHash(Hash blockHash);
        PathContextService SetAccount(Hash accountAddress);
        PathContextService SetDataProvider(Hash dataProvider);
        Hash GetPointerHash();
        Hash GetPathHash();
    }
}