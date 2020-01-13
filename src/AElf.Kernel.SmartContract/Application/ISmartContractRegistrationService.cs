using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractRegistrationService
    {
        Task AddSmartContractRegistrationAsync(Address address, Hash codeHash, BlockIndex blockIndex);
        Task<Dictionary<Address, List<Hash>>> RemoveForkCacheAsync(List<BlockIndex> blockIndexes);
        Dictionary<Address, List<Hash>> SetIrreversedCache(List<BlockIndex> blockIndexes);
        SmartContractRegistrationCache GetSmartContractRegistrationCacheFromForkCache(
            IChainContext chainContext, Address address);
        bool TryGetSmartContractRegistrationLibCache(Address address, out SmartContractRegistrationCache cache);
        void SetSmartContractRegistrationLibCache(Address address, SmartContractRegistrationCache cache);
        Task<SmartContractCodeHistory> GetSmartContractCodeHistoryAsync(Address address);
    }
}