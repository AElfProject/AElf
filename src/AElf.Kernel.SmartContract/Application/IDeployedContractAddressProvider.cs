using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IDeployedContractAddressProvider
    {
        void Init(List<Address> addresses);
        bool CheckContractAddress(Address address);
        void AddDeployedContractAddress(Address address, Hash blockHash);

        void RemoveForkCache(List<Hash> blockHashes);
        void SetIrreversedCache(List<Hash> blockHashes);
        void SetIrreversedCache(Hash blockHash);
    }
}