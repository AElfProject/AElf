using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class SmartContractManager : ISmartContractManager
    {
        private readonly IKeyValueStore _smartContractStore;

        public SmartContractManager(SmartContractStore smartContractStore)
        {
            _smartContractStore = smartContractStore;
        }

        public async Task<SmartContractRegistration> GetAsync(Address contractAddress)
        {
            return await _smartContractStore.GetAsync<SmartContractRegistration>(
                contractAddress.GetPublicKeyHash()
            );
        }

        public async Task InsertAsync(Address contractAddress, SmartContractRegistration reg)
        {
            await _smartContractStore.SetAsync(contractAddress.GetPublicKeyHash(), reg);
        }
    }
}