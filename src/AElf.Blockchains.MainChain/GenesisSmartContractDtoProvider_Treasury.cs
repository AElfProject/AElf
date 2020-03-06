using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Contracts.Treasury;
using AElf.OS.Node.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForTreasury()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                GetContractCodeByName("AElf.Contracts.Treasury"),
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