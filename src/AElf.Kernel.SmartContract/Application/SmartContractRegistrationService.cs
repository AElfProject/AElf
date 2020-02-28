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

        public SmartContractRegistrationService(ISmartContractRegistrationCacheProvider smartContractRegistrationCacheProvider, 
            ISmartContractExecutiveProvider smartContractExecutiveProvider)
        {
            _smartContractRegistrationCacheProvider = smartContractRegistrationCacheProvider;
            _smartContractExecutiveProvider = smartContractExecutiveProvider;
        }

        public Task AddSmartContractRegistrationAsync(Address address, Hash codeHash, BlockIndex blockIndex)
        {
            _smartContractRegistrationCacheProvider.AddSmartContractRegistration(address, codeHash, blockIndex);
            return Task.CompletedTask;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            var codeHashDic = _smartContractRegistrationCacheProvider.RemoveForkCache(blockIndexes);
            var addresses = codeHashDic.Keys;
            foreach (var address in addresses)
            {
                _smartContractExecutiveProvider.ClearExecutives(address, codeHashDic[address]);
            }
            return Task.CompletedTask;
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