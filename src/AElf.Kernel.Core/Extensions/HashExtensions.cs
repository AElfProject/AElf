using AElf.Types;

namespace AElf.Kernel
{
    public static class HashExtensions
    {
        public static Hash Xor(this Hash hash, Hash another)
        {
            return Hash.Xor(hash, another);
        }
    }
}