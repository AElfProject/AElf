namespace AElf.Kernel.Services
{
    public interface IAccountContextService
    {
        /// <summary>
        /// IncreasmentId++ after query
        /// </summary>
        /// <param name="accountHash"></param>
        /// <param name="chainId"></param>
        /// <param name="plusIncreasmentId"></param>
        /// <returns></returns>
        IAccountDataContext GetAccountDataContext(Hash accountHash, Hash chainId);
    }
}