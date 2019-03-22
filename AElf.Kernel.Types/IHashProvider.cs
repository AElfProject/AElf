using AElf.Common;
namespace AElf.Kernel
{
    public interface IHashProvider
    {
        Hash GetHash();
        Hash GetHashWithoutCache();
    }
}