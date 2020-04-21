using System.Collections.Generic;
using AElf.Kernel.SmartContractInitialization;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.GovernmentSystem
{
    public class AssociationContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = AssociationSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Association";

        public Dictionary<string, ByteString> GetInitializeMethodMap(byte[] contractCode)
        {
            return new Dictionary<string, ByteString>();
        }
    }
}