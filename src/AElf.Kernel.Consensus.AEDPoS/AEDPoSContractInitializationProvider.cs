using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        private readonly IAEDPoSContractInitializationDataProvider _aedPoSContractInitializationDataProvider;
        private readonly ConsensusOptions _consensusOptions;
        public Hash SystemSmartContractName => ConsensusSmartContractAddressNameProvider.Name;
        public string ContractCodeName => _consensusOptions.ConsensusContractCodeName;

        public AEDPoSContractInitializationProvider(
            IAEDPoSContractInitializationDataProvider aedPoSContractInitializationDataProvider,
            IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _aedPoSContractInitializationDataProvider = aedPoSContractInitializationDataProvider;
            _consensusOptions = consensusOptions.Value;
        }

        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            var initializationData = _aedPoSContractInitializationDataProvider.GetContractInitializationData();
            return new List<ContractInitializationMethodCall>
            {
                new ContractInitializationMethodCall
                {
                    MethodName = nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                    Params = new InitialAElfConsensusContractInput
                    {
                        PeriodSeconds = initializationData.PeriodSeconds,
                        MinerIncreaseInterval = initializationData.MinerIncreaseInterval,
                        IsSideChain = initializationData.IsSideChain
                    }.ToByteString()
                },
                new ContractInitializationMethodCall
                {
                    MethodName = nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                    Params = new MinerList
                    {
                        Pubkeys =
                        {
                            initializationData.InitialMinerList.Select(ByteStringHelper.FromHexString)
                        }
                    }.GenerateFirstRoundOfNewTerm(initializationData.MiningInterval,
                        initializationData.StartTimestamp.ToDateTime()).ToByteString()
                }
            };
        }
    }
}