using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Domain
{
    public interface ISmartContractCodeHistoryManager
    {
        Task<SmartContractCodeHistory> GetSmartContractCodeHistoryAsync(Address address);
        Task RemoveSmartContractCodeHistoryAsync(Address address);
        Task SetSmartContractCodeHistoryAsync(Address address, SmartContractCodeHistory smartContractCodeHistory);
    }
    
    public class SmartContractCodeHistoryManager : ISmartContractCodeHistoryManager,ITransientDependency
    {
        private readonly IBlockchainStore<SmartContractCodeHistory> _smartContractCodeHistoryStore;

        public SmartContractCodeHistoryManager(IBlockchainStore<SmartContractCodeHistory> smartContractCodeHistoryStore)
        {
            _smartContractCodeHistoryStore = smartContractCodeHistoryStore;
        }

        public async Task<SmartContractCodeHistory> GetSmartContractCodeHistoryAsync(Address address)
        {
            var smartContractCodeHistory = await _smartContractCodeHistoryStore.GetAsync(address.ToStorageKey());
            return smartContractCodeHistory;
        }
        
        public async Task RemoveSmartContractCodeHistoryAsync(Address address)
        {
            await _smartContractCodeHistoryStore.RemoveAsync(address.ToStorageKey());
        }

        public async Task SetSmartContractCodeHistoryAsync(Address address, SmartContractCodeHistory smartContractCodeHistory)
        {
            await _smartContractCodeHistoryStore.SetAsync(address.ToStorageKey(), smartContractCodeHistory);
        }
    }
}