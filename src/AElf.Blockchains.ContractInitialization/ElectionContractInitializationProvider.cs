using Acs0;
using AElf.Contracts.Election;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.ContractInitialization
{
    public class ElectionContractInitializationProvider : ContractInitializationProviderBase
    {
        private readonly EconomicOptions _economicOptions;
        private readonly ConsensusOptions _consensusOptions;
        
        protected override Hash ContractName { get; } = ElectionSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Election";

        public ElectionContractInitializationProvider(
            IOptionsSnapshot<EconomicOptions> economicOptions, IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
            _economicOptions = economicOptions.Value;
        }

        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
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