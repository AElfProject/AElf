using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractInitialization
{
    public interface IContractInitializationProvider
    {
        Hash SystemSmartContractName { get; }
        string ContractCodeName { get; }
        List<InitializeMethod> GetInitializeMethodList(byte[] contractCode);
    }
    
    public class InitializeMethod
    {
        public string MethodName { get; set; }

        public ByteString Params { get; set; }
    }
}