using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Kernel;
using AElf.Kernel.Types.SmartContract;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;
using Easy.MessageHub;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
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
                        if (!ValidateVictories(dpoSInformation.NewTerm.Miners))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect miners list."};
                        }
                        
                        if (!OutInValueAreNull(dpoSInformation.NewTerm.FirstRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect Out Value or In Value."};
                        }
                        
                        if (!OutInValueAreNull(dpoSInformation.NewTerm.SecondRound))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect Out Value or In Value."};
                        }
                        
                        // TODO: Validate time slots (distance == 4000 ms)
                    }
                }
                else
                {
                    // Same Round
                    if (!RoundIdMatched(dpoSInformation.CurrentRound))
                    {
                        return new ValidationResult {Success = false, Message = "Round Id not match."};
                    }

                    if (!NewOutValueFilled(dpoSInformation.CurrentRound))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect new Out Value."};
                    }
                }
            }

            return new ValidationResult {Success = true};
        }

        public int GetCountingMilliseconds(Timestamp timestamp)
        {
            // To initial this chain.
            if (!_dataHelper.TryToGetCurrentRoundInformation(out _))
            {
                return Config.InitialWaitingMilliseconds;
            }
            
            // To terminate current round.
            if (AllOutValueFilled(out var minerInformation) && _dataHelper.TryToGetMiningInterval(out var miningInterval))
            {
                return (GetExtraBlockMiningTime(miningInterval)
                            .AddMilliseconds(minerInformation.Order * miningInterval) - timestamp.ToDateTime())
                    .Milliseconds;
            }

            // To produce a normal block.
            var expect = (minerInformation.ExpectedMiningTime.ToDateTime() - timestamp.ToDateTime()).Milliseconds;
            return expect > 0 ? expect : int.MaxValue;
        }

        public byte[] GetNewConsensusInformation(byte[] extraInformation)
        {
            var extra = DPoSExtraInformation.Parser.ParseFrom(extraInformation);

            if (!_dataHelper.TryToGetCurrentRoundInformation(out _))
            {
                return new DPoSInformation
                    {NewTerm = extra.InitialMiners.ToMiners().GenerateNewTerm(extra.MiningInterval)}.ToByteArray();
            }

            if (AllOutValueFilled(out _))
            {
                return extra.ChangeTerm
                    ? new DPoSInformation {NewTerm = GenerateNextTerm()}.ToByteArray()
                    : new DPoSInformation {Forwarding = GenerateNewForwarding()}.ToByteArray();
            }

            return new DPoSInformation {CurrentRound = FillOutValue(extra.HashValue)}.ToByteArray();
        }

        public TransactionList GenerateConsensusTransactions(BlockHeader blockHeader, byte[] extraInformation)
        {
            var extra = DPoSExtraInformation.Parser.ParseFrom(extraInformation);

            // Initial term.
            return new TransactionList
            {
                Transactions = { GenerateTransaction(blockHeader, "InitialTerm", new List<object>())}
            };
        }

        #region Utilities

        private bool MinersAreSame(Round round1, Round round2)
        {
            return round1.GetMinersHash() == round2.GetMinersHash();
        }

        private bool OutInValueAreNull(Round round)
        {
            return round.RealTimeMinersInfo.Values.Any(minerInRound =>
                minerInRound.OutValue != null || minerInRound.InValue != null);
        }

        private bool ValidateVictories(Miners miners)
        {
            if (_dataHelper.TryToGetVictories(out var victories))
            {
                return victories.GetMinersHash() == miners.GetMinersHash();
            }

            return false;
        }

        private bool RoundIdMatched(Round round)
        {
            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRoundInStateDB))
            {
                return currentRoundInStateDB.RoundId == round.RoundId;
            }

            return false;
        }

        private bool NewOutValueFilled(Round round)
        {
            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRoundInStateDB))
            {
                return currentRoundInStateDB.RealTimeMinersInfo.Values.Count(info => info.OutValue != null) + 1 ==
                       round.RealTimeMinersInfo.Values.Count(info => info.OutValue != null);
            }

            return false;
        }

        private bool AllOutValueFilled(out MinerInRound minerInformation)
        {
            minerInformation = null;
            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRoundInStateDB))
            {
                var publicKey = Api.RecoverPublicKey().ToHex();
                if (currentRoundInStateDB.RealTimeMinersInfo.ContainsKey(publicKey))
                {
                    minerInformation = currentRoundInStateDB.RealTimeMinersInfo[publicKey];
                }
                return currentRoundInStateDB.RealTimeMinersInfo.Values.Count(info => info.OutValue != null) ==
                       GlobalConfig.BlockProducerNumber;
            }

            return false;
        }

        private Round GenerateNextRound()
        {
            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRoundInStateDB))
            {
                return currentRoundInStateDB.RealTimeMinersInfo.Keys.ToMiners()
                    .GenerateNextRound(currentRoundInStateDB);
            }

            return new Round();
        }
        
        private Round GenerateNextRound(Round currentRound)
        {
            return currentRound.RealTimeMinersInfo.Keys.ToMiners().GenerateNextRound(currentRound);
        }

        private Forwarding GenerateNewForwarding()
        {
            if (_dataHelper.TryToGetCurrentAge(out var blockAge))
            {
                if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRound))
                {
                    if (currentRound.RoundNumber != 1 &&
                        _dataHelper.TryToGetPreviousRoundInformation(out var previousRound))
                    {
                        return new Forwarding
                        {
                            CurrentAge = blockAge,
                            CurrentRound = currentRound.Supplement(previousRound),
                            NextRound = GenerateNextRound(currentRound)
                        };
                    }

                    if (currentRound.RoundNumber == 1)
                    {
                        return new Forwarding
                        {
                            CurrentAge = blockAge,
                            CurrentRound = currentRound.SupplementForFirstRound(),
                            NextRound = new Round {RoundNumber = 0}
                        };
                    }
                }
            }

            return new Forwarding();
        }

        private Term GenerateNextTerm()
        {
            if (_dataHelper.TryToGetTermNumber(out var termNumber))
            {
                if (_dataHelper.TryToGetRoundNumber(out var roundNumber))
                {
                    if (_dataHelper.TryToGetVictories(out var victories))
                    {
                        if (_dataHelper.TryToGetMiningInterval(out var miningInterval))
                        {
                            return victories.GenerateNewTerm(miningInterval, roundNumber, termNumber);
                        }
                    }
                }
            }

            return new Term();
        }

        private Round FillOutValue(Hash outValue)
        {
            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRoundInStateDB))
            {
                var publicKey = Api.RecoverPublicKey().ToHex();
                if (currentRoundInStateDB.RealTimeMinersInfo.ContainsKey(publicKey))
                {
                    currentRoundInStateDB.RealTimeMinersInfo[publicKey].OutValue = outValue;
                }

                return currentRoundInStateDB;
            }

            return new Round();
        }

        private DateTime GetExtraBlockMiningTime(int miningInterval)
        {
            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRoundInStateDB))
            {
                return currentRoundInStateDB.GetEBPMiningTime(miningInterval);
            }

            return DateTime.MaxValue;
        }

        private Transaction GenerateTransaction(BlockHeader blockHeader, string methodName, List<object> parameters)
        {
            var blockNumber = blockHeader.Index;
            blockNumber = blockNumber > 4 ? blockNumber - 4 : 0;
            var bh = blockNumber == 0 ? Hash.Genesis : blockHeader.GetHash();
            var blockPrefix = bh.Value.Where((x, i) => i < 4).ToArray();

            var tx = new Transaction
            {
                From = Address.FromPublicKey(Api.RecoverPublicKey()),
                To = Api.ConsensusContractAddress,
                RefBlockNumber = blockNumber,
                RefBlockPrefix = ByteString.CopyFrom(blockPrefix),
                MethodName = methodName,
                Type = TransactionType.DposTransaction,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters.ToArray()))
            };

            MessageHub.Instance.Publish(StateEvent.ConsensusTxGenerated);

            return tx;
        }

        #endregion
    }
}