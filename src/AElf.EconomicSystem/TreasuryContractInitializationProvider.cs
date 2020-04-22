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

        public List<InitializeMethod> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<InitializeMethod>
            {
                new InitializeMethod{
                    MethodName = nameof(TreasuryContractContainer.TreasuryContractStub.InitialTreasuryContract),
                    Params = new Empty().ToByteString()
                },
                new InitializeMethod{
                    MethodName = nameof(TreasuryContractContainer.TreasuryContractStub.InitialMiningRewardProfitItem),
                    Params = new Empty().ToByteString()
                }
            };
        }
    }
}