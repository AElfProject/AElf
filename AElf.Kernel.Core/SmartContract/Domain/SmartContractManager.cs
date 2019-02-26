using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContract.Domain
{
    public class SmartContractManager : ISmartContractManager
    {
        private readonly IBlockchainStore<SmartContractRegistration> _smartContractStore;

        public SmartContractManager(IBlockchainStore<SmartContractRegistration> smartContractStore)
        {
            _smartContractStore = smartContractStore;
        }

        public async Task<SmartContractRegistration> GetAsync(Hash contractHash)
        {
            return await _smartContractStore.GetAsync(contractHash.ToStorageKey());
        }

        public async Task InsertAsync(SmartContractRegistration registration)
        {
            await _smartContractStore.SetAsync(registration.CodeHash.ToStorageKey(), registration);
        }
    }
}