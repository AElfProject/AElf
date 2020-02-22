using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: remove
    public interface IDeployedContractAddressProvider
    {
        void Init(List<Address> addresses);
        bool CheckContractAddress(IChainContext chainContext, Address address);
        void AddDeployedContractAddress(Address address, BlockIndex blockIndex);
        
        //TODO: no fork check
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }
}