using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Kernel;
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
    // ReSharper disable UnusedMember.Global
    // ReSharper disable MemberCanBePrivate.Global
    public class DPoSContract : CSharpSmartContract, IConsensusSmartContract
    {
        private readonly IDPoSDataHelper _dataHelper;

        private DataStructures DataStructures => new DataStructures
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
            SnapshotMap = new Map<UInt64Value, TermSnapshot>(GlobalConfig.AElfDPoSSnapshotMapString),
            AliasesMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesMapString),
            AliasesLookupMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesLookupMapString),
            HistoryMap = new Map<StringValue, CandidateInHistory>(GlobalConfig.AElfDPoSHistoryMapString),
            AgeToRoundNumberMap = new Map<UInt64Value, UInt64Value>(GlobalConfig.AElfDPoSAgeToRoundNumberMapString),
            VotingRecordsMap = new Map<Hash, VotingRecord>("__VotingRecordsMap__"),
            TermToFirstRoundMap = new MapToUInt64<UInt64Value>("__TermToFirstRoundMap__")
        };

        private readonly Process Process;

        private readonly Election Election;

        public DPoSContract()
        {
            _dataHelper = new DPoSDataHelper(DataStructures);

            Election = new Election(DataStructures, _dataHelper);
            Process = new Process(_dataHelper);
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
            if ((AllOutValueFilled(out var minerInformation) || TimeOverflow(timestamp)) &&
                _dataHelper.TryToGetMiningInterval(out var miningInterval))
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

            // To initial consensus information.
            if (!_dataHelper.TryToGetRoundNumber(out _))
            {
                return new DPoSInformation
                {
                    WillUpdateConsensus = true,
                    Sender = Address.FromPublicKey(Api.RecoverPublicKey()),
                    NewTerm = extra.InitialMiners.ToMiners().GenerateNewTerm(extra.MiningInterval)
                }.ToByteArray();
            }

            // To terminate current round.
            if (AllOutValueFilled(out _) || TimeOverflow(extra.Timestamp))
            {
                return extra.ChangeTerm
                    ? new DPoSInformation
                    {
                        WillUpdateConsensus = true,
                        Sender = Address.FromPublicKey(Api.RecoverPublicKey()),
                        NewTerm = GenerateNextTerm(),
                    }.ToByteArray()
                    : new DPoSInformation
                    {
                        WillUpdateConsensus = true,
                        Sender = Address.FromPublicKey(Api.RecoverPublicKey()),
                        Forwarding = GenerateNewForwarding()
                    }.ToByteArray();
            }

            // To publish Out Value.
            return new DPoSInformation {CurrentRound = FillOutValue(extra.HashValue)}.ToByteArray();
        }
        
        public TransactionList GenerateConsensusTransactions(BlockHeader blockHeader, byte[] extraInformation)
        {
            var extra = DPoSExtraInformation.Parser.ParseFrom(extraInformation);

            // To initial consensus information.
            if (!_dataHelper.TryToGetRoundNumber(out _))
            {
                return new TransactionList
                {
                    Transactions =
                        {GenerateTransaction(blockHeader, "InitialTerm", new List<object> {extra.NewTerm})}
                };
            }

            // To terminate current round.
            if (AllOutValueFilled(out _) || TimeOverflow(extra.Timestamp))
            {
                if (extra.ChangeTerm && _dataHelper.TryToGetRoundNumber(out var roundNumber) &&
                    _dataHelper.TryToGetTermNumber(out var termNumber))
                {
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(blockHeader, "NextTerm", new List<object> {extra.NewTerm}),
                            GenerateTransaction(blockHeader, "SnapshotForMiners", new List<object>{roundNumber, termNumber}),
                            GenerateTransaction(blockHeader, "SnapshotForTerm", new List<object>{roundNumber, termNumber}),
                            GenerateTransaction(blockHeader, "SendDividends", new List<object>{roundNumber, termNumber})
                        }
                    };
                }

                return new TransactionList
                {
                    Transactions =
                    {
                        GenerateTransaction(blockHeader, "NextRound", new List<object> {extra.Forwarding}),
                    }
                };
            }

            // To publish Out Value.
            return new TransactionList
            {
                Transactions =
                {
                    GenerateTransaction(blockHeader, "PackageOutValue", new List<object> {extra.ToPackage}),
                }
            };
        }

        #region Process

        [Fee(0)]
        public void InitialTerm(Term term)
        {
            Api.Assert(term.FirstRound.RoundNumber == 1, "It seems that the term number of initial term is incorrect.");
            Api.Assert(term.SecondRound.RoundNumber == 2,
                "It seems that the term number of initial term is incorrect.");
            Process.InitialTerm(term);
        }

        [Fee(0)]
        public ActionResult NextTerm(Term term)
        {
            return Process.NextTerm(term);
        }

        [Fee(0)]
        public ActionResult SnapshotForMiners(ulong previousTermNumber, ulong lastRoundNumber)
        {
            return Process.SnapshotForMiners(previousTermNumber, lastRoundNumber);
        }

        [Fee(0)]
        public ActionResult SnapshotForTerm(ulong snapshotTermNumber, ulong lastRoundNumber)
        {
            return Process.SnapshotForTerm(snapshotTermNumber, lastRoundNumber);
        }

        [Fee(0)]
        public ActionResult SendDividends(ulong dividendsTermNumber, ulong lastRoundNumber)
        {
            return Process.SendDividends(dividendsTermNumber, lastRoundNumber);
        }

        [Fee(0)]
        public void NextRound(Forwarding forwarding)
        {
            Process.NextRound(forwarding);
        }

        [Fee(0)]
        public void PackageOutValue(ToPackage toPackage)
        {
            Process.PackageOutValue(toPackage);
        }

        [Fee(0)]
        public void BroadcastInValue(ToBroadcast toBroadcast)
        {
            Process.BroadcastInValue(toBroadcast);
        }

        #endregion Process

        #region Query

        [View]
        public Round GetRoundInfo(ulong roundNumber)
        {
            if (DataStructures.RoundsMap.TryGet(roundNumber.ToUInt64Value(), out var roundInfo))
            {
                return roundInfo;
            }

            return new Round
            {
                Remark = "Round information not found."
            };
        }

        [View]
        public ulong GetCurrentRoundNumber()
        {
            return DataStructures.CurrentRoundNumberField.GetValue();
        }

        [View]
        public ulong GetCurrentTermNumber()
        {
            return DataStructures.CurrentTermNumberField.GetValue();
        }

        [View]
        public bool IsCandidate(string publicKey)
        {
            return DataStructures.CandidatesField.GetValue().PublicKeys.Contains(publicKey);
        }

        [View]
        public StringList GetCandidatesList()
        {
            return DataStructures.CandidatesField.GetValue().PublicKeys.ToList().ToStringList();
        }

        [View]
        public string GetCandidatesListToFriendlyString()
        {
            return GetCandidatesList().ToString();
        }

        [View]
        public CandidateInHistory GetCandidateHistoryInfo(string publicKey)
        {
            if (DataStructures.HistoryMap.TryGet(publicKey.ToStringValue(), out var info))
            {
                return info;
            }

            return new CandidateInHistory
            {
                PublicKey = publicKey,
                ContinualAppointmentCount = 0,
                MissedTimeSlots = 0,
                ProducedBlocks = 0,
                ReappointmentCount = 0
            };
        }

        [View]
        public string GetCandidateHistoryInfoToFriendlyString(string publicKey)
        {
            return GetCandidateHistoryInfo(publicKey).ToString();
        }

        [View]
        public CandidateInHistoryDictionary GetCandidatesHistoryInfo()
        {
            var result = new CandidateInHistoryDictionary();

            var candidates = DataStructures.CandidatesField.GetValue();
            result.CandidatesNumber = candidates.PublicKeys.Count;

            foreach (var candidate in candidates.PublicKeys)
            {
                if (DataStructures.HistoryMap.TryGet(candidate.ToStringValue(), out var info))
                {
                    if (DataStructures.TicketsMap.TryGet(candidate.ToStringValue(), out var tickets))
                    {
                        info.CurrentVotesNumber = tickets.ObtainedTickets;
                    }

                    result.Maps.Add(candidate, info);
                }
            }

            return result;
        }

        [View]
        public string GetCandidatesHistoryInfoToFriendlyString()
        {
            return GetCandidatesHistoryInfo().ToString();
        }

        [View]
        public CandidateInHistoryDictionary GetPageableCandidatesHistoryInfo(int startIndex, int length)
        {
            var result = new CandidateInHistoryDictionary();

            var candidates = DataStructures.CandidatesField.GetValue();
            result.CandidatesNumber = candidates.PublicKeys.Count;

            var take = Math.Min(result.CandidatesNumber - startIndex, length - startIndex);
            foreach (var candidate in candidates.PublicKeys.Skip(startIndex).Take(take))
            {
                if (DataStructures.HistoryMap.TryGet(candidate.ToStringValue(), out var info))
                {
                    if (DataStructures.TicketsMap.TryGet(candidate.ToStringValue(), out var tickets))
                    {
                        info.CurrentVotesNumber = tickets.ObtainedTickets;
                    }

                    result.Maps.Add(candidate, info);
                }
                else
                {
                    result.Maps.Add(candidate, new CandidateInHistory {Remark = "Not found."});
                }
            }

            return result;
        }

        [View]
        public string GetPageableCandidatesHistoryInfoToFriendlyString(int startIndex, int length)
        {
            return GetPageableCandidatesHistoryInfo(startIndex, length).ToString();
        }

        [View]
        public Miners GetCurrentMiners()
        {
            var currentTermNumber = DataStructures.CurrentTermNumberField.GetValue();
            if (currentTermNumber == 0)
            {
                currentTermNumber = 1;
            }

            if (DataStructures.MinersMap.TryGet(currentTermNumber.ToUInt64Value(), out var currentMiners))
            {
                return currentMiners;
            }

            return new Miners
            {
                Remark = "Can't get current miners."
            };
        }

        [View]
        public string GetCurrentMinersToFriendlyString()
        {
            return GetCurrentMiners().ToString();
        }

        [View]
        public Tickets GetTicketsInfo(string publicKey)
        {
            if (DataStructures.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
            {
                foreach (var transactionId in tickets.VoteToTransactions)
                {
                    if (DataStructures.VotingRecordsMap.TryGet(transactionId, out var votingRecord))
                    {
                        tickets.VotingRecords.Add(votingRecord);
                    }
                }
                
                foreach (var transactionId in tickets.VoteFromTransactions)
                {
                    if (DataStructures.VotingRecordsMap.TryGet(transactionId, out var votingRecord))
                    {
                        tickets.VotingRecords.Add(votingRecord);
                    }
                }
                
                tickets.VotingRecordsCount = (ulong) tickets.VotingRecords.Count;
                return tickets;
            }

            return new Tickets();
        }

        [View]
        public string GetTicketsInfoToFriendlyString(string publicKey)
        {
            return GetTicketsInfo(publicKey).ToString();
        }

        [View]
        public ulong QueryObtainedNotExpiredVotes(string publicKey)
        {
            var tickets = GetTicketsInfo(publicKey);
            if (!tickets.VotingRecords.Any())
            {
                return 0;
            }

            return tickets.VotingRecords
                .Where(vr => vr.To == publicKey && !vr.IsExpired(DataStructures.AgeField.GetValue()))
                .Aggregate<VotingRecord, ulong>(0, (current, ticket) => current + ticket.Count);
        }

        [View]
        public ulong QueryObtainedVotes(string publicKey)
        {
            var tickets = GetTicketsInfo(publicKey);
            if (tickets.VotingRecords.Any())
            {
                return tickets.ObtainedTickets;
            }

            return 0;
        }

        [View]
        public Tickets GetPageableTicketsInfo(string publicKey, int startIndex, int length)
        {
            var tickets = GetTicketsInfo(publicKey);
            
            var count = tickets.VotingRecords.Count;
            var take = Math.Min(length - startIndex, count - startIndex);

            var result = new Tickets
            {
                VotingRecords = {tickets.VotingRecords.Skip(startIndex).Take(take)},
                ObtainedTickets = tickets.ObtainedTickets,
                VotedTickets = tickets.VotedTickets,
                HistoryObtainedTickets = tickets.HistoryObtainedTickets,
                HistoryVotedTickets = tickets.HistoryVotedTickets,
                Remark = tickets.Remark,
                VotingRecordsCount = (ulong) count,
                VoteToTransactions = {tickets.VoteToTransactions},
                VoteFromTransactions = {tickets.VoteFromTransactions}
            };

            return result;
        }

        [View]
        public string GetPageableTicketsInfoToFriendlyString(string publicKey, int startIndex, int length)
        {
            return GetPageableTicketsInfo(publicKey, startIndex, length).ToString();
        }

        [View]
        public Tickets GetPageableNotWithdrawnTicketsInfo(string publicKey, int startIndex, int length)
        {
            var tickets = GetTicketsInfo(publicKey);

            var notWithdrawnVotingRecords = tickets.VotingRecords.Where(vr => !vr.IsWithdrawn).ToList();
            var count = notWithdrawnVotingRecords.Count;
            var take = Math.Min(length - startIndex, count - startIndex);

            var result = new Tickets
            {
                VotingRecords = {notWithdrawnVotingRecords.Skip(startIndex).Take(take)},
                ObtainedTickets = tickets.ObtainedTickets,
                VotedTickets = tickets.VotedTickets,
                HistoryObtainedTickets = tickets.HistoryObtainedTickets,
                HistoryVotedTickets = tickets.HistoryVotedTickets,
                Remark = tickets.Remark,
                VotingRecordsCount = (ulong) count,
                VoteToTransactions = {tickets.VoteToTransactions},
                VoteFromTransactions = {tickets.VoteFromTransactions}
            };

            return result;
        }

        [View]
        public string GetPageableNotWithdrawnTicketsInfoToFriendlyString(string publicKey, int startIndex, int length)
        {
            return GetPageableNotWithdrawnTicketsInfo(publicKey, startIndex, length).ToString();
        }

        [View]
        public TicketsHistories GetPageableTicketsHistories(string publicKey, int startIndex, int length)
        {
            var histories = new TicketsHistories();
            var result = new TicketsHistories();
            
            var tickets = GetTicketsInfo(publicKey);

            foreach (var votingRecord in tickets.VotingRecords)
            {
                DataStructures.AliasesMap.TryGet(votingRecord.To.ToStringValue(), out var alias);
                histories.Values.Add(new TicketsHistory
                {
                    CandidateAlias = alias.Value,
                    Timestamp = votingRecord.VoteTimestamp,
                    Type = TicketsHistoryType.Vote,
                    VotesNumber = votingRecord.Count,
                    State = true
                });
                if (votingRecord.IsWithdrawn)
                {
                    histories.Values.Add(new TicketsHistory
                    {
                        CandidateAlias = alias.Value,
                        Timestamp = votingRecord.VoteTimestamp,
                        Type = TicketsHistoryType.Redeem,
                        VotesNumber = votingRecord.Count,
                        State = true
                    });
                }
            }

            var take = Math.Min(length - startIndex, histories.Values.Count - startIndex);
            result.Values.AddRange(histories.Values.Skip(startIndex).Take(take));
            result.HistoriesNumber = (ulong) histories.Values.Count;

            return result;
        }

        [View]
        public string GetPageableTicketsHistoriesToFriendlyString(string publicKey, int startIndex, int length)
        {
            return GetPageableTicketsHistories(publicKey, startIndex, length).ToString();
        }

        /// <summary>
        /// Order by:
        /// 0 - Announcement order. (Default)
        /// 1 - Obtained votes ascending.
        /// 2 - Obtained votes descending.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        [View]
        public TicketsDictionary GetPageableElectionInfo(int startIndex, int length, int orderBy)
        {
            if (orderBy == 0)
            {
                var publicKeys = DataStructures.CandidatesField.GetValue().PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                var take = Math.Min(length - startIndex, publicKeys.Count - startIndex);
                foreach (var publicKey in publicKeys.Skip(startIndex).Take(take))
                {
                    if (DataStructures.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                return dict.ToTicketsDictionary();
            }

            if (orderBy == 1)
            {
                var publicKeys = DataStructures.CandidatesField.GetValue().PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                foreach (var publicKey in publicKeys)
                {
                    if (DataStructures.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                var take = Math.Min(length - startIndex, publicKeys.Count - startIndex);
                return dict.OrderBy(p => p.Value.ObtainedTickets).Skip(startIndex).Take(take).ToTicketsDictionary();
            }

            if (orderBy == 2)
            {
                var publicKeys = DataStructures.CandidatesField.GetValue().PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                foreach (var publicKey in publicKeys)
                {
                    if (DataStructures.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                var take = Math.Min(length - startIndex, publicKeys.Count - startIndex);
                return dict.OrderByDescending(p => p.Value.ObtainedTickets).Skip(startIndex).Take(take)
                    .ToTicketsDictionary();
            }

            return new TicketsDictionary
            {
                Remark = "Failed to get election information."
            };
        }

        [View]
        public string GetPageableElectionInfoToFriendlyString(int startIndex, int length, int orderBy)
        {
            return GetPageableElectionInfo(startIndex, length, orderBy).ToString();
        }

        [View]
        public ulong GetBlockchainAge()
        {
            return DataStructures.AgeField.GetValue();
        }

        [View]
        public StringList GetCurrentVictories()
        {
            return Process.GetVictories().ToStringList();
        }

        [View]
        public string GetCurrentVictoriesToFriendlyString()
        {
            return GetCurrentVictories().ToString();
        }

        [View]
        public TermSnapshot GetTermSnapshot(ulong termNumber)
        {
            if (DataStructures.SnapshotMap.TryGet(termNumber.ToUInt64Value(), out var snapshot))
            {
                return snapshot;
            }

            return new TermSnapshot
            {
                Remark = "Invalid term number."
            };
        }

        [View]
        public string GetTermSnapshotToFriendlyString(ulong termNumber)
        {
            return GetTermSnapshot(termNumber).ToString();
        }

        [View]
        public string QueryAlias(string publicKey)
        {
            return DataStructures.AliasesMap.TryGet(new StringValue {Value = publicKey}, out var alias)
                ? alias.Value
                : publicKey.Substring(0, GlobalConfig.AliasLimit);
        }

        [View]
        public ulong GetTermNumberByRoundNumber(ulong roundNumber)
        {
            var map = DataStructures.TermNumberLookupField.GetValue().Map;
            Api.Assert(map != null, GlobalConfig.TermNumberLookupNotFound);
            return map?.OrderBy(p => p.Key).Last(p => roundNumber >= p.Value).Key ?? (ulong) 0;
        }

        [View]
        public ulong GetVotesCount()
        {
            return DataStructures.VotesCountField.GetValue();
        }

        [View]
        public ulong GetTicketsCount()
        {
            return DataStructures.TicketsCountField.GetValue();
        }

        [View]
        public ulong QueryCurrentDividendsForVoters()
        {
            return DataStructures.RoundsMap.TryGet(GetCurrentRoundNumber().ToUInt64Value(), out var roundInfo)
                ? Config.GetDividendsForVoters(roundInfo.GetMinedBlocks())
                : 0;
        }

        [View]
        public ulong QueryCurrentDividends()
        {
            return DataStructures.RoundsMap.TryGet(GetCurrentRoundNumber().ToUInt64Value(), out var roundInfo)
                ? Config.GetDividendsForAll(roundInfo.GetMinedBlocks())
                : 0;
        }

        [View]
        public StringList QueryAliasesInUse()
        {
            var candidates = DataStructures.CandidatesField.GetValue();
            var result = new StringList();
            foreach (var publicKey in candidates.PublicKeys)
            {
                if (DataStructures.AliasesMap.TryGet(publicKey.ToStringValue(), out var alias))
                {
                    result.Values.Add(alias.Value);
                }
            }

            return result;
        }

        [View]
        public ulong QueryMinedBlockCountInCurrentTerm(string publicKey)
        {
            if (DataStructures.RoundsMap.TryGet(GetCurrentRoundNumber().ToUInt64Value(), out var round))
            {
                if (round.RealTimeMinersInfo.ContainsKey(publicKey))
                {
                    return round.RealTimeMinersInfo[publicKey].ProducedBlocks;
                }
            }

            return 0;
        }

        [View]
        public string QueryAliasesInUseToFriendlyString()
        {
            return QueryAliasesInUse().ToString();
        }

        #endregion

        #region Election

        public ActionResult AnnounceElection(string alias)
        {
            return Election.AnnounceElection(alias);
        }

        public ActionResult QuitElection()
        {
            return Election.QuitElection();
        }

        public ActionResult Vote(string candidatePublicKey, ulong amount, int lockTime)
        {
            return Election.Vote(candidatePublicKey, amount, lockTime);
        }

        public ActionResult ReceiveDividendsByTransactionId(string transactionId)
        {
            return Election.ReceiveDividends(transactionId);
        }

        public ActionResult ReceiveAllDividends()
        {
            return Election.ReceiveDividends();
        }

        public ActionResult WithdrawByTransactionId(string transactionId, bool withoutLimitation)
        {
            return Election.Withdraw(transactionId, withoutLimitation);
        }

        public ActionResult WithdrawAll(bool withoutLimitation)
        {
            return Election.Withdraw(withoutLimitation);
        }

        public void InitialBalance(Address address, ulong amount)
        {
            var sender = Api.RecoverPublicKey().ToHex();
            Api.Assert(DataStructures.RoundsMap.TryGet(((ulong) 1).ToUInt64Value(), out var firstRound),
                "First round not found.");
            Api.Assert(firstRound.RealTimeMinersInfo.ContainsKey(sender),
                "Sender should be one of the initial miners.");

            Api.SendInlineByContract(Api.TokenContractAddress, "Transfer", address, amount);
        }

        #endregion Election
        
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

        private bool TimeOverflow(Timestamp timestamp)
        {
            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRoundInStateDB) &&
                _dataHelper.TryToGetMiningInterval(out var miningInterval))
            {
                return currentRoundInStateDB.GetEBPMiningTime(miningInterval) < timestamp.ToDateTime();
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
            if (_dataHelper.TryToGetCurrentAge(out var blockAge) && 
                _dataHelper.TryToGetCurrentRoundInformation(out var currentRound))
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

            return new Forwarding();
        }

        private Term GenerateNextTerm()
        {
            if (_dataHelper.TryToGetTermNumber(out var termNumber) &&
                _dataHelper.TryToGetRoundNumber(out var roundNumber) &&
                _dataHelper.TryToGetVictories(out var victories) &&
                _dataHelper.TryToGetMiningInterval(out var miningInterval))
            {
                return victories.GenerateNewTerm(miningInterval, roundNumber, termNumber);
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