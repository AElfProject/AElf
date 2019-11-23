using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface IContractRemarksManager
    {
        Task<ContractRemarks> GetContractRemarksAsync(IChainContext chainContext, Address address, Hash codeHash);
        Task SetContractRemarksAsync(Address address, ContractRemarks contractRemarks);
        void AddCodeHashCache(IBlockIndex blockIndex, Address address, Hash codeHash);
        void RemoveContractRemarksCache(List<BlockIndex> blockIndexes);
        Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes);
    }
    
    public class ContractRemarksManager : IContractRemarksManager
    {
        private readonly IBlockchainStore<ContractRemarks> _contractRemarksStore;
        private readonly IContractRemarksCacheProvider _contractRemarksProvider;

        public ContractRemarksManager(IBlockchainStore<ContractRemarks> contractRemarksStore, 
            IContractRemarksCacheProvider contractRemarksCacheProvider)
        {
            _contractRemarksStore = contractRemarksStore;
            _contractRemarksProvider = contractRemarksCacheProvider;
        }

        public async Task SetContractRemarksAsync(Address address, ContractRemarks contractRemarks)
        {
            await _contractRemarksStore.SetAsync(address.ToStorageKey(), contractRemarks);
        }

        public void AddCodeHashCache(IBlockIndex blockIndex, Address address, Hash codeHash)
        {
            _contractRemarksProvider.AddCodeHashCache(blockIndex, address, codeHash);
        }

        public async Task<ContractRemarks> GetContractRemarksAsync(IChainContext chainContext, Address address,Hash codeHash)
        {
            var contractRemarks = _contractRemarksProvider.GetContractRemarks(chainContext, address);
            if (contractRemarks != null) return codeHash == contractRemarks.CodeHash ? contractRemarks : null;
            contractRemarks = await _contractRemarksStore.GetAsync(address.ToStorageKey());
            _contractRemarksProvider.SetContractRemarks(new ContractRemarks
            {
                ContractAddress = address,
                CodeHash = contractRemarks?.CodeHash ?? codeHash,
                NonParallelizable = contractRemarks?.NonParallelizable ?? false
            });
            return codeHash == contractRemarks?.CodeHash ? contractRemarks : null;
        }

        public void RemoveContractRemarksCache(List<BlockIndex> blockIndexes)
        {
            _contractRemarksProvider.RemoveForkCache(blockIndexes);
        }

        public async Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            var contractRemarksList = _contractRemarksProvider.SetIrreversedCache(blockIndexes);
            foreach (var contractRemarks in contractRemarksList)
            {
                await SetContractRemarksAsync(contractRemarks.ContractAddress, contractRemarks);
            }
        }
    }
}