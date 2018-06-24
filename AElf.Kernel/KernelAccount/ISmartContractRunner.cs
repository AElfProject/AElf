using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractRunner
    {
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
        System.Type GetContractType(SmartContractRegistration reg);
    }
}