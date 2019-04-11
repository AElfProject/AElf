using AElf.Types;

namespace AElf.Kernel
{
    public interface IHashProvider
    {
        Hash GetHash();
        Hash GetHashWithoutCache();
    }
}