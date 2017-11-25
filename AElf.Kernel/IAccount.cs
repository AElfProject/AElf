namespace AElf.Kernel
{
    public interface IAccount
    {
        /// <summary>
        /// Get Account's Address, the address is the id for a account
        /// </summary>
        /// <returns></returns>
        byte[] GetAddress();
    }
}