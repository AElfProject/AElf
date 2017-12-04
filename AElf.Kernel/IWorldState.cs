namespace AElf.Kernel
{
    /// <summary>
    /// World State presents the state of a chain, changed by block. 
    /// </summary>
    public interface IWorldState
    {
        /// <summary>
        /// Get a data provider for an account to do further data operation 
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        IAccountDataProvider GetAccountDataProviderByAccount(IAccount account);
    }
}