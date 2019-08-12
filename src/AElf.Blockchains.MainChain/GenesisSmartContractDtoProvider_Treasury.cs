using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Treasury;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForTreasury()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("Treasury")).Value,
                TreasurySmartContractAddressNameProvider.Name,
                GenerateTreasuryInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTreasuryInitializationCallList()
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