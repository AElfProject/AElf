using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Election;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForElection(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Election")).Value,
               ElectionSmartContractAddressNameProvider.Name, GenerateElectionInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateElectionInitializationCallList()
        {
            var electionContractMethodCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            electionContractMethodCallList.Add(nameof(ElectionContractContainer.ElectionContractStub.InitialElectionContract),
                new InitialElectionContractInput
                {
                    // Create Treasury profit item and register sub items.
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    VoteContractSystemName = VoteSmartContractAddressNameProvider.Name,
                    ProfitContractSystemName = ProfitSmartContractAddressNameProvider.Name,
                    
                    // Get current miners.
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    MaximumLockTime = 1080,
                    MinimumLockTime = 90
                });
            return electionContractMethodCallList;
        }
    }
}