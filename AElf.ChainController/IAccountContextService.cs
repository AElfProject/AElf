using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.SmartContract;

namespace AElf.ChainController
{
    public interface IAccountContextService
    {
        /// <summary>
        /// return account context
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<IAccountDataContext> GetAccountDataContext(Address accountAddress, Hash chainId);

        /// <summary>
        /// set incrementId in memory
        /// and wait for inserting to storage
        /// </summary>
        /// <param name="accountDataContext"></param>
        Task SetAccountContext(IAccountDataContext accountDataContext);

    }
}