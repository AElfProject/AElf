using System;
using System.Linq;
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
        public ValidationResult ValidateConsensus(byte[] consensusInformation)
        {
            var dpoSInformation = DPoSInformation.Parser.ParseFrom(consensusInformation);

            if (_dataHelper.IsMiner(dpoSInformation.Sender))
            {
                return new ValidationResult {Success = false, Message = "Sender is not a miner."};
            }

            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRound))
            {
                if (dpoSInformation.WillUpdateConsensus)
                {
                    if (dpoSInformation.Forwarding != null)
                    {
                        // Next Round
                        if (!MinersAreSame(currentRound, dpoSInformation.Forwarding.NextRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect miners list."};
                        }

                        if (!OutInValueAreNull(dpoSInformation.Forwarding.NextRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect Out Value or In Value."};
                        }

                        // TODO: Validate time slots (distance == 4000 ms)
                    }

                    if (dpoSInformation.NewTerm != null)
                    {
                        // Next Term
                        
                    }
                }
                else
                {
                    // Same Round
                    
                }
            }


            return new ValidationResult {Success = true};
        }

        public ulong GetCountingMilliseconds(Timestamp timestamp)
        {
            throw new NotImplementedException();
        }

        public byte[] GetNewConsensusInformation()
        {
            throw new NotImplementedException();
        }

        public TransactionList GenerateConsensusTransactions()
        {
            throw new NotImplementedException();
        }

        private Address GetAddress(string publicKeyToHex)
        {
            return Address.FromPublicKey(ByteArrayHelpers.FromHexString(publicKeyToHex));
        }

        private bool MinersAreSame(Round round1, Round round2)
        {
            return round1.MinersHash() == round2.MinersHash();
        }

        private bool OutInValueAreNull(Round round)
        {
            return round.RealTimeMinersInfo.Values.Any(minerInRound =>
                minerInRound.OutValue != null || minerInRound.InValue != null);
        }

        private bool ValidateVictories()
        {
            throw new NotImplementedException();
        }
    }
}