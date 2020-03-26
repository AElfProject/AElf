using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Election;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForElection()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                GetContractCodeByName("AElf.Contracts.Election"),
                ElectionSmartContractAddressNameProvider.Name, GenerateElectionInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateElectionInitializationCallList()
        {
            var electionContractMethodCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            electionContractMethodCallList.Add(
                nameof(ElectionContractContainer.ElectionContractStub.InitialElectionContract),
                new InitialElectionContractInput
                {
                    MaximumLockTime = _economicOptions.MaximumLockTime,
                    MinimumLockTime = _economicOptions.MinimumLockTime,
                    TimeEachTerm = _consensusOptions.PeriodSeconds,
                    MinerList = {_consensusOptions.InitialMinerList},
                    MinerIncreaseInterval = _consensusOptions.MinerIncreaseInterval
                });
            return electionContractMethodCallList;
        }
    }
}