using System.Collections.Generic;
using AElf.Contracts.Parliament;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContractInitialization;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.GovernmentSystem
{
    public class ParliamentContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = ParliamentSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Parliament";

        private readonly IParliamentContractInitializationDataProvider _parliamentContractInitializationDataProvider;

        public ParliamentContractInitializationProvider(
            IParliamentContractInitializationDataProvider parliamentContractInitializationDataProvider)
        {
            _parliamentContractInitializationDataProvider = parliamentContractInitializationDataProvider;
        }

        public List<InitializeMethod> GetInitializeMethodList(byte[] contractCode)
        {
            var initializationData = _parliamentContractInitializationDataProvider.GetContractInitializationData();
            return new List<InitializeMethod>
            {
                new InitializeMethod{
                    MethodName = nameof(ParliamentContractContainer.ParliamentContractStub.Initialize),
                    Params = new InitializeInput
                    {
                        PrivilegedProposer = initializationData.PrivilegedProposer,
                        ProposerAuthorityRequired = initializationData.ProposerAuthorityRequired
                    }.ToByteString()
                }
            };
        }
    }
}