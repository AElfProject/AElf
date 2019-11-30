using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface IContractRemarksManager
    {
        Task<ContractRemarks> GetContractRemarksAsync(Address address);
        Task RemoveContractRemarksAsync(Address address);
        Task SetContractRemarksAsync(Address address, ContractRemarks contractRemarks);
    }
    
    public class ContractRemarksManager : IContractRemarksManager
    {
        private readonly IBlockchainStore<ContractRemarks> _contractRemarksStore;

        public ContractRemarksManager(IBlockchainStore<ContractRemarks> contractRemarksStore)
        {
            _contractRemarksStore = contractRemarksStore;
        }

        public async Task<ContractRemarks> GetContractRemarksAsync(Address address)
        {
            var contractRemarks = await _contractRemarksStore.GetAsync(address.ToStorageKey());
            return contractRemarks;
        }
        
        public async Task RemoveContractRemarksAsync(Address address)
        {
            await _contractRemarksStore.RemoveAsync(address.ToStorageKey());
        }

        public async Task SetContractRemarksAsync(Address address, ContractRemarks contractRemarks)
        {
            await _contractRemarksStore.SetAsync(address.ToStorageKey(), contractRemarks);
        }
    }
}