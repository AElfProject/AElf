using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public static class WorldState
    {
        private static Dictionary<IHash, ISerializable> _dataProviders = new Dictionary<IHash, ISerializable>();

        #region Get Account Data Provider
        /// <summary>
        /// If the data provider of given hash exists, return the provider,
        /// otherwise create a new provider with nothing.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static IAccountDataProvider GetAccountDataProvider(IHash address)
        {
            ISerializable result;
            if (_dataProviders.TryGetValue(address, out result))
            {
                return (IAccountDataProvider)result.Deserialize();
            }
            else
            {
                var defaultAccountDataProvider = new AccountDataProvider();
                _dataProviders.Add(address, defaultAccountDataProvider);
                return defaultAccountDataProvider;
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
            ISerializable result;
            if (_dataProviders.TryGetValue(providerAddress, out result))
            {
                return (IDataProvider)result.Deserialize();
            }
            else
            {
                var defaultDataProvider = new DataProvider();
                _dataProviders.Add(providerAddress, defaultDataProvider);
                return defaultDataProvider;
            }
        }
        #endregion

        #region Set Account Data Provider
        public static bool SetAccountDataProvider(IHash address, ISerializable accountDataProvider)
        {
            if (_dataProviders.ContainsKey(address))
            {
                //If the account data provider already exists, shouldn't set its value directly.
                return false;
            }
            //Validation
            if (accountDataProvider.GetType() != typeof(AccountDataProvider))
            {
                return false;
            }
            _dataProviders[address] = accountDataProvider;
            return true;
        }
        #endregion

        #region Set Data Provider
        public static bool SetDataProvider(IHash providerAddress, ISerializable dataProvider)
        {
            //Validation
            if (dataProvider.GetType() != typeof(DataProvider))
            {
                return false;
            }
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
