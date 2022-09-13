using System.Collections.Generic;
using AElf.Contracts.CrossChain;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain;

public class CrossChainContractInitializationProvider : IContractInitializationProvider, ITransientDependency
{
    private readonly ICrossChainContractInitializationDataProvider _crossChainContractInitializationDataProvider;

    public CrossChainContractInitializationProvider(
        ICrossChainContractInitializationDataProvider crossChainContractInitializationDataProvider)
    {
        _crossChainContractInitializationDataProvider = crossChainContractInitializationDataProvider;
    }

    public Hash SystemSmartContractName { get; } = CrossChainSmartContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "AElf.Contracts.CrossChain";

    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        var initializationData = _crossChainContractInitializationDataProvider.GetContractInitializationData();
        return new List<ContractInitializationMethodCall>
        {
            new()
            {
                MethodName = nameof(CrossChainContractImplContainer.CrossChainContractImplStub.Initialize),
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