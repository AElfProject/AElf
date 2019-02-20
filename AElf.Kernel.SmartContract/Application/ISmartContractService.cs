using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractService
    {
        
        /// <summary>
        /// Deploys a contract to the specified chain and account.
        /// </summary>
        /// <param name="chainId">The chain id for the contract to be deployed in.</param>
        /// <param name="contractAddress">The target address for the contract.</param>
        /// <param name="registration">The contract registration info.</param>
        /// <param name="isPrivileged">Whether the contract is a privileged (system) one.</param>
        /// <returns></returns>
        Task DeployContractAsync(int chainId, Address contractAddress, SmartContractRegistration registration, bool isPrivileged);

        Task UpdateContractAsync(int chainId, Address contractAddress, SmartContractRegistration registration, bool isPrivileged);

        Task DeployZeroContractAsync(int chainId, SmartContractRegistration registration);

        
//        Task<IExecutive> GetExecutiveAsync(Address contractAddress, int chainId);
//        Task PutExecutiveAsync(int chainId, Address account, IExecutive executive);
//
//
//
//        Task<IMessage> GetAbiAsync(int chainId, Address account);
//
//        Task<SmartContractRegistration> GetContractByAddressAsync(int chainId, Address address);


    }
}