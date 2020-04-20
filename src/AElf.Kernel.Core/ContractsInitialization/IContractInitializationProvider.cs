using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.ContractsInitialization
{
    public interface IContractInitializationProvider
    {
        Hash SystemSmartContractName { get; }
        string ContractCodeName { get; }
        Dictionary<string, ByteString> GetInitializeMethodMap(byte[] contractCode);
    }
}