using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractCodeHistoryService
    {
        Task<SmartContractCodeHistory> GetSmartContractCodeHistoryAsync(Address address);
        Task AddSmartContractCodeAsync(Address address, Hash codeHash, BlockIndex blockIndex);
        Task RemoveAsync(List<BlockIndex> blockIndexes);
    }
    
    public class SmartContractCodeHistoryService : ISmartContractCodeHistoryService, ITransientDependency
    {
        private readonly ISmartContractCodeHistoryProvider _smartContractCodeHistoryProvider;
        private readonly ISmartContractCodeHistoryManager _smartContractCodeHistoryManager;

        public SmartContractCodeHistoryService(ISmartContractCodeHistoryProvider smartContractCodeHistoryProvider,
            ISmartContractCodeHistoryManager smartContractCodeHistoryManager)
        {
            _smartContractCodeHistoryProvider = smartContractCodeHistoryProvider;
            _smartContractCodeHistoryManager = smartContractCodeHistoryManager;
        }

        public async Task<SmartContractCodeHistory> GetSmartContractCodeHistoryAsync(Address address)
        {
            var smartContractCodeHistory = _smartContractCodeHistoryProvider.GetSmartContractCodeHistory(address);
            if (smartContractCodeHistory != null) return smartContractCodeHistory;
            smartContractCodeHistory = await _smartContractCodeHistoryManager.GetSmartContractCodeHistoryAsync(address);
            if(smartContractCodeHistory != null)
                _smartContractCodeHistoryProvider.SetSmartContractCodeHistory(address, smartContractCodeHistory);
            return smartContractCodeHistory;
        }

        public async Task AddSmartContractCodeAsync(Address address, Hash codeHash, BlockIndex blockIndex)
        {
            var smartContractCodeHistory = await GetSmartContractCodeHistoryAsync(address) ??
                                           new SmartContractCodeHistory();
            _smartContractCodeHistoryProvider.AddSmartContractCode(address, codeHash, blockIndex);
            smartContractCodeHistory.Codes.AddIfNotContains(new SmartContractCode
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight,
                CodeHash = codeHash
            });
            await _smartContractCodeHistoryManager.SetSmartContractCodeHistoryAsync(address, smartContractCodeHistory);
        }

        public async Task RemoveAsync(List<BlockIndex> blockIndexes)
        {
            var smartContractCodeDic = _smartContractCodeHistoryProvider.Remove(blockIndexes);
            foreach (var keyPair in smartContractCodeDic)
            {
                if(keyPair.Value.Count == 0) continue;
                var address = keyPair.Key;
                var smartContractCodeHistory = await _smartContractCodeHistoryManager.GetSmartContractCodeHistoryAsync(address);
                foreach (var smartContractCode in keyPair.Value)
                {
                    smartContractCodeHistory.Codes.Remove(smartContractCode);
                }

                if (smartContractCodeHistory.Codes.Count == 0)
                    await _smartContractCodeHistoryManager.RemoveSmartContractCodeHistoryAsync(address);
                else
                    await _smartContractCodeHistoryManager.SetSmartContractCodeHistoryAsync(address, smartContractCodeHistory);
            }
        }
    }
}