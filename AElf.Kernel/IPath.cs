namespace AElf.Kernel
{
    /// <summary>
    /// There exists a Pointer class in System.Reflection namespace,
    /// so use "Path" to avoid the misleading.
    /// </summary>
    public interface IPath
    {
        Path SetChainHash(Hash chainHash);
        Path SetBlockHash(Hash blockHash);
        Path SetAccount(Hash accountAddress);
        Path SetDataProvider(Hash dataProvider);
        Hash GetPointerHash();
        Hash GetPathHash();
    }
}