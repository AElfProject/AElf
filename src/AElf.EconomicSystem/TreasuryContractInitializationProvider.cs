using System.Collections.Generic;
using AElf.Contracts.Treasury;
using AElf.Kernel.SmartContractInitialization;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.EconomicSystem
{
    public class TreasuryContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        public Hash SystemSmartContractName { get; } = TreasurySmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Treasury";

        public Dictionary<string, ByteString> GetInitializeMethodMap(byte[] contractCode)
        {
            return new Dictionary<string, ByteString>
            {
                {
                    nameof(TreasuryContractContainer.TreasuryContractStub.InitialTreasuryContract),
                    new Empty().ToByteString()
                },
                {
                    nameof(TreasuryContractContainer.TreasuryContractStub.InitialMiningRewardProfitItem),
                    new Empty().ToByteString()
                }
            };
        }
    }
}