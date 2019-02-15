using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using AElf.Contracts.Consensus.Contracts;
using AElf.Kernel;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Global
    public class ConsensusContract : CSharpSmartContract
    {
        private DataCollection Collection => new DataCollection
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
            // TODO: To implement.
            TwoThirdsMinersMinedCurrentTermField = new BoolField(GlobalConfig.AElfTwoThirdsMinerMinedString),

            RoundsMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSRoundsMapString),
            MinersMap = new Map<UInt64Value, Miners>(GlobalConfig.AElfDPoSMinersMapString),
            TicketsMap = new Map<StringValue, Tickets>(GlobalConfig.AElfDPoSTicketsMapString),
            SnapshotField = new Map<UInt64Value, TermSnapshot>(GlobalConfig.AElfDPoSSnapshotMapString),
            AliasesMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesMapString),
            AliasesLookupMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesLookupMapString),
            HistoryMap = new Map<StringValue, CandidateInHistory>(GlobalConfig.AElfDPoSHistoryMapString),
            AgeToRoundNumberMap = new Map<UInt64Value, UInt64Value>(GlobalConfig.AElfDPoSAgeToRoundNumberMapString),
            VotingRecordsMap = new Map<Hash, VotingRecord>(GlobalConfig.AElfDPoSVotingRecordsMapString)
        };

        private Process Process => new Process(Api.ChainId, Collection);

        private Election Election => new Election(Collection);

        private Validation Validation => new Validation(Collection);

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
            if (Collection.RoundsMap.TryGet(roundNumber.ToUInt64Value(), out var roundInfo))
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
            return Collection.CurrentRoundNumberField.GetValue();
        }

        [View]
        public ulong GetCurrentTermNumber()
        {
            return Collection.CurrentTermNumberField.GetValue();
        }

        [View]
        public bool IsCandidate(string publicKey)
        {
            return Collection.CandidatesField.GetValue().PublicKeys.Contains(publicKey);
        }

        [View]
        public StringList GetCandidatesList()
        {
            return Collection.CandidatesField.GetValue().PublicKeys.ToList().ToStringList();
        }

        [View]
        public string GetCandidatesListToFriendlyString()
        {
            return GetCandidatesList().ToString();
        }

        [View]
        public CandidateInHistory GetCandidateHistoryInfo(string publicKey)
        {
            if (Collection.HistoryMap.TryGet(publicKey.ToStringValue(), out var info))
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

            var candidates = Collection.CandidatesField.GetValue();
            result.CandidatesNumber = candidates.PublicKeys.Count;

            foreach (var candidate in candidates.PublicKeys)
            {
                if (Collection.HistoryMap.TryGet(candidate.ToStringValue(), out var info))
                {
                    if (Collection.TicketsMap.TryGet(candidate.ToStringValue(), out var tickets))
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

            var candidates = Collection.CandidatesField.GetValue();
            result.CandidatesNumber = candidates.PublicKeys.Count;

            var take = Math.Min(result.CandidatesNumber - startIndex, length - startIndex);
            foreach (var candidate in candidates.PublicKeys.Skip(startIndex).Take(take))
            {
                if (Collection.HistoryMap.TryGet(candidate.ToStringValue(), out var info))
                {
                    if (Collection.TicketsMap.TryGet(candidate.ToStringValue(), out var tickets))
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
            var currentTermNumber = Collection.CurrentTermNumberField.GetValue();
            if (currentTermNumber == 0)
            {
                currentTermNumber = 1;
            }

            if (Collection.MinersMap.TryGet(currentTermNumber.ToUInt64Value(), out var currentMiners))
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

        // TODO: Add an API to get unexpired tickets info.
        [View]
        public Tickets GetTicketsInfo(string publicKey)
        {
            if (Collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
            {
                foreach (var transactionId in tickets.VoteToTransactions)
                {
                    if (Collection.VotingRecordsMap.TryGet(transactionId, out var votingRecord))
                    {
                        tickets.VotingRecords.Add(votingRecord);
                    }
                }
                
                foreach (var transactionId in tickets.VoteFromTransactions)
                {
                    if (Collection.VotingRecordsMap.TryGet(transactionId, out var votingRecord))
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
                .Where(vr => vr.To == publicKey && !vr.IsExpired(Collection.AgeField.GetValue()))
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
                Collection.AliasesMap.TryGet(votingRecord.To.ToStringValue(), out var alias);
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
                var publicKeys = Collection.CandidatesField.GetValue().PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                var take = Math.Min(length - startIndex, publicKeys.Count - startIndex);
                foreach (var publicKey in publicKeys.Skip(startIndex).Take(take))
                {
                    if (Collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                return dict.ToTicketsDictionary();
            }

            if (orderBy == 1)
            {
                var publicKeys = Collection.CandidatesField.GetValue().PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                foreach (var publicKey in publicKeys)
                {
                    if (Collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                var take = Math.Min(length - startIndex, publicKeys.Count - startIndex);
                return dict.OrderBy(p => p.Value.ObtainedTickets).Skip(startIndex).Take(take).ToTicketsDictionary();
            }

            if (orderBy == 2)
            {
                var publicKeys = Collection.CandidatesField.GetValue().PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                foreach (var publicKey in publicKeys)
                {
                    if (Collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
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
            return Collection.AgeField.GetValue();
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
            if (Collection.SnapshotField.TryGet(termNumber.ToUInt64Value(), out var snapshot))
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
            return Collection.AliasesMap.TryGet(new StringValue {Value = publicKey}, out var alias)
                ? alias.Value
                : publicKey.Substring(0, GlobalConfig.AliasLimit);
        }

        [View]
        public ulong GetTermNumberByRoundNumber(ulong roundNumber)
        {
            var map = Collection.TermNumberLookupField.GetValue().Map;
            Api.Assert(map != null, GlobalConfig.TermNumberLookupNotFound);
            return map?.OrderBy(p => p.Key).Last(p => roundNumber >= p.Value).Key ?? (ulong) 0;
        }

        [View]
        public ulong GetVotesCount()
        {
            return Collection.VotesCountField.GetValue();
        }

        [View]
        public ulong GetTicketsCount()
        {
            return Collection.TicketsCountField.GetValue();
        }

        [View]
        public ulong QueryCurrentDividendsForVoters()
        {
            return Collection.RoundsMap.TryGet(GetCurrentRoundNumber().ToUInt64Value(), out var roundInfo)
                ? Config.GetDividendsForVoters(roundInfo.GetMinedBlocks())
                : 0;
        }

        [View]
        public ulong QueryCurrentDividends()
        {
            return Collection.RoundsMap.TryGet(GetCurrentRoundNumber().ToUInt64Value(), out var roundInfo)
                ? Config.GetDividendsForAll(roundInfo.GetMinedBlocks())
                : 0;
        }

        [View]
        public StringList QueryAliasesInUse()
        {
            var candidates = Collection.CandidatesField.GetValue();
            var result = new StringList();
            foreach (var publicKey in candidates.PublicKeys)
            {
                if (Collection.AliasesMap.TryGet(publicKey.ToStringValue(), out var alias))
                {
                    result.Values.Add(alias.Value);
                }
            }

            return result;
        }

        [View]
        public ulong QueryMinedBlockCountInCurrentTerm(string publicKey)
        {
            if (Collection.RoundsMap.TryGet(GetCurrentRoundNumber().ToUInt64Value(), out var round))
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
            Api.Assert(Collection.RoundsMap.TryGet(((ulong) 1).ToUInt64Value(), out var firstRound),
                "First round not found.");
            Api.Assert(firstRound.RealTimeMinersInfo.ContainsKey(sender),
                "Sender should be one of the initial miners.");

            Api.SendInlineByContract(Api.TokenContractAddress, "Transfer", address, amount);
        }

        #endregion Election

        #region Validation

        public BlockValidationResult ValidateBlock(BlockAbstract blockAbstract)
        {
            return Validation.ValidateBlock(blockAbstract);
        }

        #endregion Validation
    }
}