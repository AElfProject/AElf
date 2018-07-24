using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public class SmartContractStore : ISmartContractStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public SmartContractStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash hash, SmartContractRegistration registration)
        {
            var key = hash.GetKeyString(TypeName.TnSmartContractRegistration);
            await _keyValueDatabase.SetAsync(key, registration.Serialize());
        }

        public async Task<SmartContractRegistration> GetAsync(Hash hash)
        {
            var key = hash.GetKeyString(TypeName.TnSmartContractRegistration);
            var bytes = await _keyValueDatabase.GetAsync(key, typeof(SmartContractRegistration));
            return SmartContractRegistration.Parser.ParseFrom(bytes);
        }
    }
}
