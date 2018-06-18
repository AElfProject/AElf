// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    /// <summary>
    /// Every smart contract was an account
    /// </summary>
    public interface IAccount 
    {
        /// <summary>
        /// Get Account's Hash value
        /// </summary>
        /// <returns></returns>
        Hash GetAccountHash();

        /// <summary>
        /// Get Account's address
        /// </summary>
        /// <returns></returns>
        byte[] GetAddress();

        /// <summary>
        /// Get address's hex string
        /// </summary>
        /// <returns></returns>
        string GetAddressHex();
    }
}