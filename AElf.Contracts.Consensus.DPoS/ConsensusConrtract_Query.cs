using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract
    {
        [View]
        public Round GetRoundInformation(ulong roundNumber)
        {
            return TryToGetRoundInformation(roundNumber, out var roundInfo) ? roundInfo : null;
        }

        [View]
        public ulong GetCurrentRoundNumber()
        {
            return State.CurrentRoundNumberField.Value;
        }

        [View]
        public ulong GetCurrentTermNumber()
        {
            return State.CurrentTermNumberField.Value;
        }

        [View]
        public bool IsCandidate(string publicKey)
        {
            return State.CandidatesField.Value.PublicKeys.Contains(publicKey);
        }

        [View]
        public StringList GetCandidatesList()
        {
            return new StringList
            {
                Values = {State.CandidatesField.Value.PublicKeys.ToList()}
            };
        }

        [View]
        public string GetCandidatesListToFriendlyString()
        {
            return GetCandidatesList().ToString();
        }

        [View]
        public CandidateInHistory GetCandidateHistoryInformation(string publicKey)
        {
            var historyInformation = State.HistoryMap[publicKey.ToStringValue()];

            return historyInformation ?? new CandidateInHistory
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
            return GetCandidateHistoryInformation(publicKey).ToString();
        }

        [View]
        public CandidateInHistoryDictionary GetCandidatesHistoryInfo()
        {
            var result = new CandidateInHistoryDictionary();

            var candidates = State.CandidatesField.Value;
            result.CandidatesNumber = candidates.PublicKeys.Count;

            foreach (var candidate in candidates.PublicKeys)
            {
                var historyInformation = State.HistoryMap[candidate.ToStringValue()];
                if (historyInformation == null)
                {
                    return result;
                }

                var tickets = State.TicketsMap[candidate.ToStringValue()];
                if (tickets == null)
                {
                    return result;
                }
                
                historyInformation.CurrentVotesNumber = tickets.ObtainedTickets;
                result.Maps.Add(candidate, historyInformation);
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

            var candidates = State.CandidatesField.Value;
            result.CandidatesNumber = candidates.PublicKeys.Count;

            var take = Math.Min(result.CandidatesNumber - startIndex, length - startIndex);
            foreach (var candidate in candidates.PublicKeys.Skip(startIndex).Take(take))
            {
                var historyInformation = State.HistoryMap[candidate.ToStringValue()];
                if (historyInformation == null)
                {
                    result.Maps.Add(candidate, new CandidateInHistory {Remark = "Not found."});
                    return result;
                }

                var tickets = State.TicketsMap[candidate.ToStringValue()];
                if (tickets == null)
                {
                    result.Maps.Add(candidate, new CandidateInHistory {Remark = "Not found."});
                    return result;
                }

                historyInformation.CurrentVotesNumber = tickets.ObtainedTickets;
                result.Maps.Add(candidate, historyInformation);
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
            var currentTermNumber = State.CurrentTermNumberField.Value;
            if (currentTermNumber == 0)
            {
                currentTermNumber = 1;
            }

            var currentMiners = State.MinersMap[currentTermNumber.ToUInt64Value()];

            return currentMiners;
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
            var tickets = State.TicketsMap[publicKey.ToStringValue()];

            if (tickets == null)
            {
                return null;
            }
            
            foreach (var transactionId in tickets.VoteToTransactions)
            {
                var votingRecord = State.VotingRecordsMap[transactionId];
                if (votingRecord != null)
                {
                    tickets.VotingRecords.Add(votingRecord);

                }
            }
                
            foreach (var transactionId in tickets.VoteFromTransactions)
            {
                var votingRecord = State.VotingRecordsMap[transactionId];
                if (votingRecord != null)
                {
                    tickets.VotingRecords.Add(votingRecord);
                }
            }
                
            tickets.VotingRecordsCount = (ulong) tickets.VotingRecords.Count;
            return tickets;
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
                .Where(vr => vr.To == publicKey && !vr.IsExpired(State.AgeField.Value))
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
                var alias = State.AliasesMap[votingRecord.To.ToStringValue()];
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
                var publicKeys = State.CandidatesField.Value.PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                var take = Math.Min(length - startIndex, publicKeys.Count - startIndex);
                foreach (var publicKey in publicKeys.Skip(startIndex).Take(take))
                {
                    var tickets = State.TicketsMap[publicKey.ToStringValue()];
                    if (tickets != null)
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                return new TicketsDictionary
                {
                    Maps = {dict},
                    
                };
            }

            if (orderBy == 1)
            {
                var publicKeys = State.CandidatesField.Value.PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                foreach (var publicKey in publicKeys)
                {
                    var tickets = State.TicketsMap[publicKey.ToStringValue()];
                    if (tickets != null)
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                var take = Math.Min(length - startIndex, publicKeys.Count - startIndex);
                return new TicketsDictionary
                {
                    Maps = {dict.OrderBy(p => p.Value.ObtainedTickets).Skip(startIndex).Take(take).ToDictionary(p => p.Key, p => p.Value)}
                };
            }

            if (orderBy == 2)
            {
                var publicKeys = State.CandidatesField.Value.PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }

                var dict = new Dictionary<string, Tickets>();
                foreach (var publicKey in publicKeys)
                {
                    var tickets = State.TicketsMap[publicKey.ToStringValue()];
                    if (tickets != null)
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                var take = Math.Min(length - startIndex, publicKeys.Count - startIndex);
                return new TicketsDictionary
                {
                    Maps = {dict.OrderByDescending(p => p.Value.ObtainedTickets).Skip(startIndex).Take(take).ToDictionary(p => p.Key, p => p.Value)}
                };
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
            return State.AgeField.Value;
        }

        [View]
        public StringList GetCurrentVictories()
        {
            return TryToGetVictories(out var victories)
                ? new StringList {Values = {victories.PublicKeys}}
                : new StringList();
        }

        [View]
        public string GetCurrentVictoriesToFriendlyString()
        {
            return GetCurrentVictories().ToString();
        }

        [View]
        public TermSnapshot GetTermSnapshot(ulong termNumber)
        {
            return State.SnapshotMap[termNumber.ToUInt64Value()];
        }

        [View]
        public string GetTermSnapshotToFriendlyString(ulong termNumber)
        {
            return GetTermSnapshot(termNumber).ToString();
        }

        [View]
        public string QueryAlias(string publicKey)
        {
            var alias = State.AliasesMap[publicKey.ToStringValue()];
            return alias == null ? publicKey.Substring(0, DPoSContractConsts.AliasLimit) : alias.Value;
        }

        [View]
        public ulong GetTermNumberByRoundNumber(ulong roundNumber)
        {
            var map = State.TermNumberLookupField.Value.Map;
            Assert(map != null, "Term number not found.");
            return map?.OrderBy(p => p.Key).Last(p => roundNumber >= p.Value).Key ?? (ulong) 0;
        }

        [View]
        public ulong GetVotesCount()
        {
            return State.VotesCountField.Value;
        }

        [View]
        public ulong GetTicketsCount()
        {
            return State.TicketsCountField.Value;
        }

        [View]
        public ulong QueryCurrentDividendsForVoters()
        {
            var minedBlocks = State.RoundsMap[GetCurrentRoundNumber().ToUInt64Value()].GetMinedBlocks();
            return (ulong) (minedBlocks * DPoSContractConsts.VotersRatio) * DPoSContractConsts.ElfTokenPerBlock;
        }

        [View]
        public ulong QueryCurrentDividends()
        {
            var minedBlocks = State.RoundsMap[GetCurrentRoundNumber().ToUInt64Value()].GetMinedBlocks();
            return minedBlocks * DPoSContractConsts.ElfTokenPerBlock;
        }

        [View]
        public StringList QueryAliasesInUse()
        {
            var candidates = State.CandidatesField.Value;
            var result = new StringList();
            foreach (var publicKey in candidates.PublicKeys)
            {
                var alias = State.AliasesMap[publicKey.ToStringValue()];
                if (alias != null)
                {
                    result.Values.Add(alias.Value);
                }
            }

            return result;
        }

        [View]
        public ulong QueryMinedBlockCountInCurrentTerm(string publicKey)
        {
            var round = State.RoundsMap[GetCurrentRoundNumber().ToUInt64Value()];
            if (round != null)
            {
                if (round.RealTimeMinersInformation.ContainsKey(publicKey))
                {
                    return round.RealTimeMinersInformation[publicKey].ProducedBlocks;
                }
            }

            return 0;
        }

        [View]
        public string QueryAliasesInUseToFriendlyString()
        {
            return QueryAliasesInUse().ToString();
        }
    }
}