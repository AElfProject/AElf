using System;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Types.SmartContract;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSContract : IConsensusSmartContract
    {
        private readonly IDPoSDataHelper _dataHelper;

        public DPoSContract()
        {
            _dataHelper = new DPoSDataHelper(new DataStructures
            {
                CurrentRoundNumberField = new UInt64Field(GlobalConfig.AElfDPoSCurrentRoundNumber),
                MiningIntervalField = new Int32Field(GlobalConfig.AElfDPoSMiningIntervalString),
                CandidatesField = new PbField<Candidates>(GlobalConfig.AElfDPoSCandidatesString),
                TermNumberLookupField = new PbField<TermNumberLookUp>(GlobalConfig.AElfDPoSTermNumberLookupString),
                AgeField = new UInt64Field(GlobalConfig.AElfDPoSAgeFieldString),
                CurrentTermNumberField = new UInt64Field(GlobalConfig.AElfDPoSCurrentTermNumber),
                BlockchainStartTimestamp = new PbField<Timestamp>(GlobalConfig.AElfDPoSBlockchainStartTimestamp),
                VotesCountField = new UInt64Field(GlobalConfig.AElfVotesCountString),
                TicketsCountField = new UInt64Field(GlobalConfig.AElfTicketsCountString),

                RoundsMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSRoundsMapString),
                MinersMap = new Map<UInt64Value, Miners>(GlobalConfig.AElfDPoSMinersMapString),
                TicketsMap = new Map<StringValue, Tickets>(GlobalConfig.AElfDPoSTicketsMapString),
                SnapshotField = new Map<UInt64Value, TermSnapshot>(GlobalConfig.AElfDPoSSnapshotMapString),
                AliasesMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesMapString),
                AliasesLookupMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesLookupMapString),
                HistoryMap = new Map<StringValue, CandidateInHistory>(GlobalConfig.AElfDPoSHistoryMapString),
                AgeToRoundNumberMap = new Map<UInt64Value, UInt64Value>(GlobalConfig.AElfDPoSAgeToRoundNumberMapString),
                VotingRecordsMap = new Map<Hash, VotingRecord>(GlobalConfig.AElfDPoSVotingRecordsMapString)
            });
        }
        
        [View]
        public bool ValidateConsensus(byte[] consensusInformation)
        {
            var information = DPoSInformation.Parser.ParseFrom(consensusInformation);

            Api.Assert(_dataHelper.IsMiner(information.Sender), "Sender isn't a miner.");

            if (_dataHelper.TryToGetCurrentRoundInformation(out var roundInformation))
            {
                if (information.WillUpdateConsensus)
                {
                    
                }
            }
            
            
            return false;
        }

        public ulong GetCountingMilliseconds()
        {
            throw new NotImplementedException();
        }

        public byte[] GetNewConsensusInformation()
        {
            throw new NotImplementedException();
        }

        private Address GetAddress(string publicKeyToHex)
        {
            return Address.FromPublicKey(ByteArrayHelpers.FromHexString(publicKeyToHex));
        }
    }
}