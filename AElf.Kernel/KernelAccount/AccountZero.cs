using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public class AccountZero : IAccount
    {
        private readonly SmartContractZero _smartContractZero;
        private static bool _isInititalized = true;
        private readonly WorldState _worldState;

        public AccountZero(SmartContractZero smartContractZero, WorldState worldState)
        {
            _smartContractZero = smartContractZero;
            _worldState = worldState;
        }
        
        /// <summary>
        /// Inititalize for accountZero
        /// </summary>
        /// <returns></returns>
        private bool Inititalize()
        {
            if (_isInititalized) return false;
            
            // get accountZeroDataProvider and set the "SmartContractMap" dataProvider
            var accountZeroDataProvider = _worldState.GetAccountDataProviderByAccount(this);
            accountZeroDataProvider.GetDataProvider().SetDataProvider("SmartContractMap", new DataProvider(this));
            _isInititalized = true;
            return true;
        }

        /// <summary>
        /// deploy contracts in genesis block
        /// </summary>
        /// <param name="smartContractRegistrations"></param>
        public void DeployContractsInGenesinGenesisBlock(IEnumerable<SmartContractRegistration> smartContractRegistrations)
        {
            if(!Inititalize())
                return;
            var tasks = new List<Task>();
            foreach (var sm in smartContractRegistrations)
            {
                Task task = Task.Factory.StartNew(async () =>
                    {
                        await _smartContractZero.RegisterSmartContract(sm);
                    }
                );
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            
        }

        
        public IHash<IAccount> GetAddress()
        {
            return Hash<IAccount>.Zero;
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}