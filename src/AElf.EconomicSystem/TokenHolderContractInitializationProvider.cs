using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.EconomicSystem
{
    public class TokenHolderContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = TokenHolderSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.TokenHolder";
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }
    }
}