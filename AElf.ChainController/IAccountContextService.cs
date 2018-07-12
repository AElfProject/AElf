using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.SmartContract;

namespace AElf.ChainController
{
    public interface IAccountContextService
    {
        /// <summary>
        /// return account context
        /// </summary>
        /// <param name="accountHash"></param>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<IAccountDataContext> GetAccountDataContext(Hash accountHash, Hash chainId);

        /// <summary>
        /// set incrementId in memory
        /// and wait for inserting to storage
        /// </summary>
        /// <param name="accountDataContext"></param>
        Task SetAccountContext(IAccountDataContext accountDataContext);

    }
}