using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public static class WorldState
    {
        private static Dictionary<IHash, byte[]> _dataProviders = new Dictionary<IHash, byte[]>();

        #region Get Account Data Provider
        /// <summary>
        /// If the data provider of given hash exists, return the provider,
        /// otherwise create a new provider with nothing.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static IAccountDataProvider GetAccountDataProvider(IHash address)
        {
            byte[] result;
            if (_dataProviders.TryGetValue(address, out result))
            {
                return (IAccountDataProvider)result.ToObject();
            }
            else
            {
                return null;
            }
        }

        public static IAccountDataProvider GetAccountDataProviderByAccount(IAccount account)
        {
            return GetAccountDataProvider(account.GetAddress());
        }
        #endregion

        #region Get Data Provider
        public static IDataProvider GetDataProvider(IHash providerAddress)
        {
            byte[] result;
            if (_dataProviders.TryGetValue(providerAddress, out result))
            {
                return (IDataProvider)result.ToObject();
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Set Account Data Provider
        public static bool SetAccountDataProvider(IHash address, byte[] accountDataProvider)
        {
            if (_dataProviders.ContainsKey(address))
            {
                //If the account data provider already exists, shouldn't set its value directly.
                return false;
            }
            _dataProviders[address] = accountDataProvider;
            return true;
        }
        #endregion

        #region Set Data Provider
        public static bool SetDataProvider(IHash providerAddress, byte[] dataProvider)
        {
            _dataProviders[providerAddress] = dataProvider;
            return true;
        }
        #endregion

        public static Task<IHash<IMerkleTree<IHash>>> GetWordStateMerkleTreeRootAsync()
        {
            throw new NotImplementedException();
        }
    }
}
