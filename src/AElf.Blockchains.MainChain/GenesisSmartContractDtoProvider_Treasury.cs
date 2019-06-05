using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Treasury;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForTreasury(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Treasury")).Value,
                ElectionSmartContractAddressNameProvider.Name, GenerateTreasuryInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateTreasuryInitializationCallList()
        {
            var treasuryContractMethodCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            treasuryContractMethodCallList.Add(nameof(TreasuryContractContainer.TreasuryContractStub.InitialTreasuryContract),
                new InitialTreasuryContractInput());
            treasuryContractMethodCallList.Add(nameof(TreasuryContractContainer.TreasuryContractStub.InitialMiningRewardProfitItem),
                new InitialMiningRewardProfitItemInput());
            return treasuryContractMethodCallList;
        }
    }
}