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
        
        public Task<SmartContractRegistration> GetAsync(Hash chainId, Hash account)
        {
            throw new System.NotImplementedException();
        }

        public Task InsertAsync(SmartContractRegistration reg)
        {
            throw new System.NotImplementedException();
        }
    }
}