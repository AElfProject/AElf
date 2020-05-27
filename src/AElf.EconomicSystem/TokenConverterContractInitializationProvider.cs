using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.EconomicSystem
{
    public class TokenConverterContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = TokenConverterSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.TokenConverter";

        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }
    }
}