using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Kernel.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface ISmartContractRunner
    {
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
        IMessage GetAbi(SmartContractRegistration reg);
        System.Type GetContractType(SmartContractRegistration reg);
        void CodeCheck(byte[] code, bool isPrivileged=false);
    }
}