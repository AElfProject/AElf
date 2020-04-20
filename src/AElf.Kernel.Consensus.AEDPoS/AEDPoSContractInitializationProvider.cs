using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.ContractsInitialization;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSContractInitializationProvider : IContractInitializationProvider
    {
        private readonly ConsensusOptions _consensusOptions;
        public Hash SystemSmartContractName => ConsensusSmartContractAddressNameProvider.Name;
        public string ContractCodeName => "AElf.Contracts.Consensus.AEDPoS";

        public AEDPoSContractInitializationProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
        }

        public Dictionary<string, ByteString> GetInitializeMethodMap(byte[] contractCode)
        {
            return new Dictionary<string, ByteString>
            {
                {
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                    new InitialAElfConsensusContractInput
                    {
                        PeriodSeconds = _consensusOptions.PeriodSeconds,
                        MinerIncreaseInterval = _consensusOptions.MinerIncreaseInterval
                    }.ToByteString()
                },
                {
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                    new MinerList
                    {
                        Pubkeys =
                        {
                            _consensusOptions.InitialMinerList.Select(p => p.ToByteString())
                        }
                    }.GenerateFirstRoundOfNewTerm(_consensusOptions.MiningInterval,
                        _consensusOptions.StartTimestamp.ToDateTime()).ToByteString()
                }
            };
        }
    }
}