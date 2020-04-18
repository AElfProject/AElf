using Acs0;
using AElf.Contracts.Treasury;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.ContractInitialization
{
    public class TreasuryContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = TreasurySmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Treasury";

        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var treasuryContractMethodCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            treasuryContractMethodCallList.Add(
                nameof(TreasuryContractContainer.TreasuryContractStub.InitialTreasuryContract),
                new Empty());
            treasuryContractMethodCallList.Add(
                nameof(TreasuryContractContainer.TreasuryContractStub.InitialMiningRewardProfitItem),
                new Empty());
            return treasuryContractMethodCallList;
        }
    }
}