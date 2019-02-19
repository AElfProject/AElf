using AElf.Common;

namespace AElf.Kernel
{
    public static class HashExtensions
    {
        /// <summary>
        /// Checks if a <see cref="Hash"/> instance is null.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static bool IsNull(this Hash hash)
        {
            return hash == null || hash.ToHex().RemoveHexPrefix().Length == 0;
        }
    }
}