using System.Collections.Generic;
using AElf.Contracts.Consensus.PoA;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.PoA;

public class PoAContractInitializationProvider : IContractInitializationProvider, ITransientDependency
{
    public Hash SystemSmartContractName => ConsensusSmartContractAddressNameProvider.Name;
    public string ContractCodeName => "AElf.Contracts.Consensus.PoA";

    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>
        {
            new()
            {
                MethodName = nameof(PoAContractContainer.PoAContractStub.Initialize),
                Params = new InitializeInput
                {
                    MiningInterval = 4000
                }.ToByteString()
            }
        };
    }
}