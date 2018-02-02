using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public static class WorldState
    {
        private static Dictionary<IHash, ISerializable> _dataProviders = new Dictionary<IHash, ISerializable>();

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
                var defaultAccountDataProvider = new AccountDataProvider().Serialize();
                _dataProviders.Add(address, defaultAccountDataProvider);
                return (IAccountDataProvider)defaultAccountDataProvider.Deserialize();
            }
        }

        public static bool SetAccountDataProvider(IHash address, ISerializable accountDataProvider)
        {
            if (_dataProviders.ContainsKey(address))
            {
                //If the account data provider already exists, shouldn't set its value directly.
                return false;
            }
            _dataProviders[address] = accountDataProvider;
            return true;
        }

        public static IDataProvider GetDataProvider(IHash accountAddress, string providerName)
        {
            var _accountDataProvider = GetAccountDataProvider(accountAddress);
            return _accountDataProvider.GetMapAsync(providerName).Result;
        }

        public static Task<IHash<IMerkleTree<IHash>>> GetWordStateMerkleTreeRootAsync()
        {
            throw new NotImplementedException();
        }
    }
}
