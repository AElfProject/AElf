using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public class SmartContractManager : ISmartContractManager
    {
        private readonly IDataStore _dataStore;

        public SmartContractManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task<SmartContractRegistration> GetAsync(Address contractAddress)
        {
            return await _dataStore.GetAsync<SmartContractRegistration>(
                Hash.FromMessage(contractAddress)
            );
        }

        public async Task InsertAsync(Address contractAddress, SmartContractRegistration reg)
        {
            await _dataStore.InsertAsync(Hash.FromMessage(contractAddress), reg);
        }
    }
}