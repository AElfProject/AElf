using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using AElf.Kernel.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractRunner
    {
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
        IMessage GetAbi(SmartContractRegistration reg);
        System.Type GetContractType(SmartContractRegistration reg);
        void CodeCheck(byte[] code, bool isPrivileged=false);
    }
}