using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Token
{
    public class TokenContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = TokenSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.MultiToken";

        private readonly ITokenContractInitializationDataProvider _tokenContractInitializationDataProvider;

        public TokenContractInitializationProvider(
            ITokenContractInitializationDataProvider tokenContractInitializationDataProvider)
        {
            _tokenContractInitializationDataProvider = tokenContractInitializationDataProvider;
        }

        public virtual List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }
    }
}