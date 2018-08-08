using System;
using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface ISmartContractService
    {
        Task<IExecutive> GetExecutiveAsync(Hash account, Hash chainId);
        Task PutExecutiveAsync(Hash account, IExecutive executive);
        /// <summary>
        /// Deploys a contract to the specified chain and account.
        /// </summary>
        /// <param name="chainId">The chain id for the contract to be deployed in.</param>
        /// <param name="account">The target address for the contract.</param>
        /// <param name="registration">The contract registration info.</param>
        /// <param name="isPrivileged">Whether the contract is a privileged (system) one.</param>
        /// <returns></returns>
        Task DeployContractAsync(Hash chainId, Hash account, SmartContractRegistration registration, bool isPrivileged);
        Type GetContractType(SmartContractRegistration registration);
        Task<IMessage> GetAbiAsync(Hash account, string name = null);
    }
}