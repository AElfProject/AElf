using System.Collections.Generic;
using AElf.Contracts.Election;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.OS;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.GovernmentSystem
{
    public class ElectionContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        private readonly EconomicOptions _economicOptions;
        private readonly ConsensusOptions _consensusOptions;

        public Hash SystemSmartContractName { get; } = ElectionSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Election";


        public ElectionContractInitializationProvider(
            IOptionsSnapshot<EconomicOptions> economicOptions, IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
            _economicOptions = economicOptions.Value;
        }

        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>
            {
                new ContractInitializationMethodCall{
                    MethodName = nameof(ElectionContractContainer.ElectionContractStub.InitialElectionContract),
                    Params = new InitialElectionContractInput
                    {
                        MaximumLockTime = _economicOptions.MaximumLockTime,
                        MinimumLockTime = _economicOptions.MinimumLockTime,
                        TimeEachTerm = _consensusOptions.PeriodSeconds,
                        MinerList = {_consensusOptions.InitialMinerList},
                        MinerIncreaseInterval = _consensusOptions.MinerIncreaseInterval
                    }.ToByteString()
                }
            };
        }
    }
}