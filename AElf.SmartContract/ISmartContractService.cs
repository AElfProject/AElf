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
        Task<IExecutive> GetExecutiveAsync(Address contractAddress, Hash chainId);
        Task PutExecutiveAsync(Address account, IExecutive executive);
        /// <summary>
        /// Deploys a contract to the specified chain and account.
        /// </summary>
        /// <param name="chainId">The chain id for the contract to be deployed in.</param>
        /// <param name="contractAddress">The target address for the contract.</param>
        /// <param name="registration">The contract registration info.</param>
        /// <param name="isPrivileged">Whether the contract is a privileged (system) one.</param>
        /// <returns></returns>
        Task DeployContractAsync(Hash chainId, Address contractAddress, SmartContractRegistration registration, bool isPrivileged);

        Task UpdateContractAsync(Hash chainId, Address contractAddress, SmartContractRegistration registration, bool isPrivileged);
        
        Task<IMessage> GetAbiAsync(Address account);
        
        /// <summary>
        /// return invoking parameters in one tx
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetInvokingParams(Transaction transaction);

        void ClearPool(Address address);
    }
}