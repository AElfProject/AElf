using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class SmartContractRegistrationStore : ISmartContractRegistrationStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public SmartContractRegistrationStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
        
        public async Task<SmartContractRegistration> GetAsync(Hash chainId, Hash account)
        {
            return (SmartContractRegistration) await _keyValueDatabase.GetAsync(CalculateContactHash(chainId, account),
                typeof(SmartContractRegistration));
        }

        public async Task InsertAsync(SmartContractRegistration reg)
        {
            await _keyValueDatabase.SetAsync(reg.ContractHash, reg);
        }

        private Hash CalculateContactHash(Hash chainId, Hash accountHash)
        {
            //TODO: The way to calculate ContractHash by chainId and accountHash
            throw new NotImplementedException();
        }
    }
}