using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Kernel;
using AElf.Common;

namespace AElf.SmartContract
{
    public interface ISmartContractService
    {
        Task<IExecutive> GetExecutiveAsync(Address contractAddress, int chainId);
        Task PutExecutiveAsync(int chainId, Address account, IExecutive executive);
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

        Task<IMessage> GetAbiAsync(int chainId, Address account);

        /// <summary>
        /// return invoking parameters in one tx
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
//        Task<IEnumerable<string>> GetInvokingParams(Hash chainId, Transaction transaction);

        Task<SmartContractRegistration> GetContractByAddressAsync(int chainId, Address address);

        Task DeployZeroContractAsync(int chainId, SmartContractRegistration registration);

        Task<Address> DeploySystemContractAsync(int chainId, SmartContractRegistration registration);
    }
}