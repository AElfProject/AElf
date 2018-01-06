using System.Security.Cryptography;

namespace AElf.Kernel.Merkle
{
    public static class MerkleExtensionMethods
    {
        public static byte[] ComputeHash(this byte[] buffer)
        {
            return SHA256.Create().ComputeHash(buffer);
        }
    }
}
