using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: remove
    public interface ISmartContractRegistrationService
    {
        Task AddSmartContractRegistrationAsync(Address address, Hash codeHash, BlockIndex blockIndex);
        Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }
    
    public class SmartContractRegistrationService : ISmartContractRegistrationService, ITransientDependency
    {
        private readonly ISmartContractRegistrationCacheProvider _smartContractRegistrationCacheProvider;
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;
        private readonly ISmartContractCodeHistoryService _smartContractCodeHistoryService;

        public SmartContractRegistrationService(ISmartContractRegistrationCacheProvider smartContractRegistrationCacheProvider, 
            ISmartContractExecutiveProvider smartContractExecutiveProvider,
            ISmartContractCodeHistoryService smartContractCodeHistoryService)
        {
            _smartContractRegistrationCacheProvider = smartContractRegistrationCacheProvider;
            _smartContractExecutiveProvider = smartContractExecutiveProvider;
            _smartContractCodeHistoryService = smartContractCodeHistoryService;
        }

        public async Task AddSmartContractRegistrationAsync(Address address, Hash codeHash, BlockIndex blockIndex)
        {
            _smartContractRegistrationCacheProvider.AddSmartContractRegistration(address, codeHash, blockIndex);
            await _smartContractCodeHistoryService.AddSmartContractCodeAsync(address, codeHash, blockIndex);
        }

        public async Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            var codeHashDic = _smartContractRegistrationCacheProvider.RemoveForkCache(blockIndexes);
            var addresses = codeHashDic.Keys;
            foreach (var address in addresses)
            {
                _smartContractExecutiveProvider.ClearExecutives(address, codeHashDic[address]);
            }

            await _smartContractCodeHistoryService.RemoveAsync(blockIndexes);
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            var codeHashDic = _smartContractRegistrationCacheProvider.SetIrreversedCache(blockIndexes);
            var addresses = codeHashDic.Keys;
            foreach (var address in addresses)
            {
                _smartContractExecutiveProvider.ClearExecutives(address, codeHashDic[address]);
            }
        }
    }
}