using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContractInitialization;
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
        public Hash SystemSmartContractName => ConsensusSmartContractAddressNameProvider.Name;
        public string ContractCodeName => "AElf.Contracts.Consensus.AEDPoS";

        public AEDPoSContractInitializationProvider(
            IAEDPoSContractInitializationDataProvider aedPoSContractInitializationDataProvider)
        {
            _aedPoSContractInitializationDataProvider = aedPoSContractInitializationDataProvider;
        }

        public Dictionary<string, ByteString> GetInitializeMethodMap(byte[] contractCode)
        {
            var initializationData = _aedPoSContractInitializationDataProvider.GetContractInitializationData();
            return new Dictionary<string, ByteString>
            {
                {
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                    new InitialAElfConsensusContractInput
                    {
                        PeriodSeconds = initializationData.PeriodSeconds,
                        MinerIncreaseInterval = initializationData.MinerIncreaseInterval,
                        IsSideChain = initializationData.IsSideChain
                    }.ToByteString()
                },
                {
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                    new MinerList
                    {
                        Pubkeys =
                        {
                            initializationData.InitialMinerList.Select(p => p.ToByteString())
                        }
                    }.GenerateFirstRoundOfNewTerm(initializationData.MiningInterval,
                        initializationData.StartTimestamp.ToDateTime()).ToByteString()
                }
            };
        }
    }
}