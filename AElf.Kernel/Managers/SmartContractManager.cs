using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class SmartContractManager : ISmartContractManager
    {
        private readonly ISmartContractStore _smartContractStore;

        public SmartContractManager(ISmartContractStore smartContractStore)
        {
            _smartContractStore = smartContractStore;
        }

        public async Task<SmartContractRegistration> GetAsync(Address contractAddress)
        {
            // Todo get contract hash from contract map
            var contractHash = new Hash();
            return await GetAsync(contractHash);
        }

        public async Task InsertAsync(Address contractAddress, SmartContractRegistration reg)
        {
            await InsertAsync(reg);
        }
        
        public async Task<SmartContractRegistration> GetAsync(Hash contractHash)
        {
            return await _smartContractStore.GetAsync<SmartContractRegistration>(contractHash.ToHex());
        }

        public async Task InsertAsync(SmartContractRegistration registration)
        {
            await _smartContractStore.SetAsync(registration.ContractHash.ToHex(), registration);
        }
    }
}