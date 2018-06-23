namespace AElf.Kernel
{
    /// <summary>
    /// Every smart contract was an account
    /// </summary>
    public interface IAccount 
    {
        /// <summary>
        /// Get Account's Address, the address is the id for a account
        /// </summary>
        /// <returns></returns>
        Hash GetAddress();
    }
}