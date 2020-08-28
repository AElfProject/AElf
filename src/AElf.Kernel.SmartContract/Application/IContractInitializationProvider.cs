using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IContractInitializationProvider
    {
        Hash SystemSmartContractName { get; }
        string ContractCodeName { get; }
        List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode);
    }
    
    public class ContractInitializationMethodCall
    {
        public string MethodName { get; set; }

        public ByteString Params { get; set; }
    }
}