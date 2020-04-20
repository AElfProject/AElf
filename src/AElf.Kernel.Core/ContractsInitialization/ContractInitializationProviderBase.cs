using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.ContractsInitialization
{
    public abstract class ContractInitializationProviderBase : IContractInitializationProvider
    {
        public abstract Hash SystemSmartContractName { get; }
        public abstract string ContractCodeName { get; }

        public Dictionary<string, ByteString> GetInitializeMethodMap(byte[] contractCode)
        {
            return new Dictionary<string, ByteString>();
        }
    }
}