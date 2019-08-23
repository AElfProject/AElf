using System.Threading.Tasks;

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
        Task DeployContractAsync(ContractDto contractDto);

        Task UpdateContractAsync(ContractDto contractDto);


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