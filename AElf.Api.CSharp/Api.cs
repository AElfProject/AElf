using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Api.CSharp
{
    /// <summary>
    /// Singleton that holds the smart contract API for interacting with the chain via the injected context.
    /// </summary>
    public class Api
    {
        private static Dictionary<string, IDataProvider> _dataProviders;
        private static IDataProvider _dataProvider;
        private static SmartContractRuntimeContext _context;

        #region Setters used by runner and executor

        public static void SetDataProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
            _dataProviders = new Dictionary<string, IDataProvider>()
            {
                {"", _dataProvider}
            };
        }
        
        public static void SetContext(SmartContractRuntimeContext context)
        {
            _context = context;
        }        

        #endregion Setters used by runner and executor

        #region Getters used by contract
        
        public static Hash GetChainId()
        {
            return _context.ChainId;
        }

        public static Hash GetContractAddress()
        {
            return _context.ContractAddress;
        }

        public static IDataProvider GetDataProvider(string name)
        {
            if (!_dataProviders.TryGetValue(name, out var dp))
            {
                dp = _dataProvider.GetDataProvider(name);
                _dataProviders.Add(name, dp);
            }

            return dp;
        }        

        #endregion Getters used by contract

    }
}