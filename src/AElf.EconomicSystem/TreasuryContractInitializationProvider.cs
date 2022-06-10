using System.Collections.Generic;
using AElf.Contracts.Treasury;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.EconomicSystem;

public class TreasuryContractInitializationProvider : IContractInitializationProvider, ITransientDependency
{
    public Hash SystemSmartContractName { get; } = TreasurySmartContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "AElf.Contracts.Treasury";

    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>
        {
            new()
            {
                MethodName = nameof(TreasuryContractContainer.TreasuryContractStub.InitialTreasuryContract),
                Params = ByteString.Empty
            },
            new()
            {
                MethodName = nameof(TreasuryContractContainer.TreasuryContractStub.InitialMiningRewardProfitItem),
                Params = ByteString.Empty
            }
        };
    }
}