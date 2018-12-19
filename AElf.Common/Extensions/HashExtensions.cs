namespace AElf.Common
{
    public static partial class Extensions
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