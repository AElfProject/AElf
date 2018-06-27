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
            await _keyValueDatabase.SetAsync(hash.Value.ToBase64(), registration.Serialize());
        }

        public async Task<SmartContractRegistration> GetAsync(Hash hash)
        {
            var bytes = await _keyValueDatabase.GetAsync(hash.Value.ToBase64(), typeof(SmartContractRegistration));
            return SmartContractRegistration.Parser.ParseFrom(bytes);
        }
    }
}
