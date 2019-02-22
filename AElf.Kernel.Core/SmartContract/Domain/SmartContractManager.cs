using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.SmartContract.Domain
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
            return await _smartContractStore.GetAsync<SmartContractRegistration>(contractHash.ToHex());
        }

        public async Task InsertAsync(SmartContractRegistration registration)
        {
            await _smartContractStore.SetAsync(registration.CodeHash.ToHex(), registration);
        }
    }
}