using System;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Services
{
    public interface ISmartContractService
    {
        Task<IExecutive> GetExecutiveAsync(Hash account, Hash chainId);
        Task PutExecutiveAsync(Hash account, IExecutive executive);
        Task DeployContractAsync(Hash chainId, Hash account, SmartContractRegistration registration);
        Type GetContractType(SmartContractRegistration registration);
        Task<IMessage> GetAbiAsync(Hash account);
    }
}