using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    internal interface IDeployedContractAddressProvider
    {
        Task<AddressList> GetDeployedContractAddressListAsync();
        void AddDeployedContractAddress(Address address);
    }
}