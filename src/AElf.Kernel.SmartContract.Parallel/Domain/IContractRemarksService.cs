using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface IContractRemarksService
    {
        Task<CodeRemark> GetCodeRemarkAsync(IChainContext chainContext, Address address, Hash codeHash);
        Task SetCodeRemarkAsync(Address address, Hash codeHash, BlockHeader blockHeader);
        Task RemoveContractRemarksCacheAsync(List<BlockIndex> blockIndexes);
        Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes);
    }
    
    public class ContractRemarksService : IContractRemarksService
    {
        private readonly IContractRemarksManager _contractRemarksManager;
        private readonly IContractRemarksCacheProvider _contractRemarksProvider;
        private readonly IBlockchainService _blockchainService;

        public ContractRemarksService(IContractRemarksCacheProvider contractRemarksProvider, IContractRemarksManager contractRemarksManager, IBlockchainService blockchainService)
        {
            _contractRemarksProvider = contractRemarksProvider;
            _contractRemarksManager = contractRemarksManager;
            _blockchainService = blockchainService;
        }

        public async Task<CodeRemark> GetCodeRemarkAsync(IChainContext chainContext, Address address, Hash codeHash)
        {
            var codeRemark = _contractRemarksProvider.GetCodeRemark(chainContext, address);
            if (codeRemark != null) return codeHash == codeRemark.CodeHash ? codeRemark : null;
            var contractRemarks = await _contractRemarksManager.GetContractRemarksAsync(address);
            if (contractRemarks != null)
            {
                var chain = await _blockchainService.GetChainAsync();
                var codeRemarks = contractRemarks.CodeRemarks
                    .Where(c => c.BlockHeight <= chain.LastIrreversibleBlockHeight)
                    .OrderByDescending(c => c.BlockHeight).ToList();
                foreach (var remark in codeRemarks)
                {
                    var blockHash = await _blockchainService.GetBlockHashByHeightAsync(chain, remark.BlockHeight,
                        chain.LastIrreversibleBlockHash);
                    if (blockHash != remark.BlockHash) continue;
                    codeRemark = remark;
                    break;
                }
            }

            if (codeRemark == null)
                codeRemark = new CodeRemark
                {
                    CodeHash = codeHash,
                    NonParallelizable = false
                };
            
            _contractRemarksProvider.SetCodeRemark(address, codeRemark);
            return codeHash == codeRemark.CodeHash ? codeRemark : null;
        }

        public async Task SetCodeRemarkAsync(Address address, Hash codeHash, BlockHeader blockHeader)
        {
            var contractRemarks = await _contractRemarksManager.GetContractRemarksAsync(address) ?? new ContractRemarks
            {
                ContractAddress = address
            };
            var codeRemark = new CodeRemark
            {
                BlockHash = blockHeader.GetHashWithoutCache(),
                BlockHeight = blockHeader.Height,
                CodeHash = codeHash,
                NonParallelizable = true
            };
            contractRemarks.CodeRemarks.AddIfNotContains(codeRemark);
            await _contractRemarksManager.SetContractRemarksAsync(address, contractRemarks);
            _contractRemarksProvider.AddCodeRemark(address, codeRemark);
        }

        public async Task RemoveContractRemarksCacheAsync(List<BlockIndex> blockIndexes)
        {
            var codeRemarksDic = _contractRemarksProvider.RemoveForkCache(blockIndexes);
            foreach (var keyPair in codeRemarksDic)
            {
                if(keyPair.Value.Count == 0) continue;
                var address = keyPair.Key;
                var contractRemarks = await _contractRemarksManager.GetContractRemarksAsync(address);
                foreach (var codeRemark in keyPair.Value)
                {
                    contractRemarks.CodeRemarks.Remove(codeRemark);
                }

                if (contractRemarks.CodeRemarks.Count == 0)
                    await _contractRemarksManager.RemoveContractRemarksAsync(address);
                else
                    await _contractRemarksManager.SetContractRemarksAsync(address, contractRemarks);
            }
        }

        public async Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            var chain = await _blockchainService.GetChainAsync();
            var codeRemarkDic = _contractRemarksProvider.SetIrreversedCache(blockIndexes);
            var addresses = codeRemarkDic.Keys.ToList();
            foreach (var address in addresses)
            {
                var contractRemarks = await _contractRemarksManager.GetContractRemarksAsync(address) ??
                                      new ContractRemarks
                                      {
                                          ContractAddress = address
                                      };
                var contractList = contractRemarks.CodeRemarks;
                contractList.RemoveAll(c => c.BlockHeight <= chain.LastIrreversibleBlockHeight);
                contractList.AddIfNotContains(codeRemarkDic[address]);
                await _contractRemarksManager.SetContractRemarksAsync(address, contractRemarks);
            }
        }
    }
}