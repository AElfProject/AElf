using System.Collections.Generic;
using AElf.Contracts.CrossChain;
using AElf.Kernel.SmartContractInitialization;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = CrossChainSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.CrossChain";

        private readonly ICrossChainContractInitializationDataProvider _crossChainContractInitializationDataProvider;

        public CrossChainContractInitializationProvider(
            ICrossChainContractInitializationDataProvider crossChainContractInitializationDataProvider)
        {
            _crossChainContractInitializationDataProvider = crossChainContractInitializationDataProvider;
        }

        public List<InitializeMethod> GetInitializeMethodList(byte[] contractCode)
        {
            var initializationData = _crossChainContractInitializationDataProvider.GetContractInitializationData();
            return new List<InitializeMethod>
            {
                new InitializeMethod{
                    MethodName = nameof(CrossChainContractContainer.CrossChainContractStub.Initialize),
                    Params = new InitializeInput
                    {
                        ParentChainId = initializationData.ParentChainId,
                        CreationHeightOnParentChain = initializationData.CreationHeightOnParentChain,
                        IsPrivilegePreserved = initializationData.IsPrivilegePreserved
                    }.ToByteString()
                }
            };
        }
    }
}