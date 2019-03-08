using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractService
    {
        /// <summary>
        /// Deploys a contract to the specified chain and account.
        /// </summary>
        /// <param name="">The chain id for the contract to be deployed in.</param>
        /// <param name="contractAddress">The target address for the contract.</param>
        /// <param name="registration">The contract registration info.</param>
        /// <param name="isPrivileged">Whether the contract is a privileged (system) one.</param>
        /// <returns></returns>
        Task DeployContractAsync(Address contractAddress, SmartContractRegistration registration, bool isPrivileged,
            Hash name = null);

        Task UpdateContractAsync(Address contractAddress, SmartContractRegistration registration, bool isPrivileged,
            Hash name = null);


//        Task<IExecutive> GetExecutiveAsync(Address contractAddress, );
//        Task PutExecutiveAsync(Address account, IExecutive executive);
//
//
//
//        Task<IMessage> GetAbiAsync(Address account);
//
//        Task<SmartContractRegistration> GetContractByAddressAsync(Address address);
    }
}