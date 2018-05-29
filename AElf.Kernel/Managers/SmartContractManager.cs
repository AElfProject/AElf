using System.Threading.Tasks;
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

        public async Task<SmartContractRegistration> GetAsync(Hash contractHash)
        {
            return await _smartContractStore.GetAsync(contractHash);
        }

        public async Task InsertAsync(Hash address, SmartContractRegistration reg)
        {
            await _smartContractStore.InsertAsync(address, reg);
        }
    }
}