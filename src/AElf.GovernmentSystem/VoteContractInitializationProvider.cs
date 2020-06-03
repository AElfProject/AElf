using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.GovernmentSystem
{
    public class VoteContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = VoteSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Vote";

        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }
    }
}