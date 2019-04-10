using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract
    {
        public override Round GetRoundInformation(SInt64Value input)
        {
            return TryToGetRoundInformation(input.Value, out var roundInfo) ? roundInfo : null;
        }

        public override SInt64Value GetCurrentRoundNumber(Empty input)
        {
            return new SInt64Value {Value = State.CurrentRoundNumberField.Value};
        }

        public override Round GetCurrentRoundInformation(Empty input)
        {
            return TryToGetRoundNumber(out var roundNumber) ? State.RoundsMap[roundNumber.ToInt64Value()] : null;
        }

        public override SInt64Value GetCurrentTermNumber(Empty input)
        {
            return new SInt64Value {Value = State.CurrentTermNumberField.Value};
        }

        public override BoolValue IsCandidate(PublicKey input)
        {
            return new BoolValue {Value = State.CandidatesField.Value.PublicKeys.Contains(input.Hex)};
        }

        public override StringList GetCandidatesList(Empty input)
        {
            var candidates = State.CandidatesField.Value;
            return candidates == null ? new StringList() : new StringList {Values = {candidates.PublicKeys.ToList()}};
        }

        public override Candidates GetCandidates(Empty input)
        {
            return State.CandidatesField.Value;
        }

        public override FriendlyString GetCandidatesListToFriendlyString(Empty input)
        {
            return new FriendlyString {Value = GetCandidatesList(input).ToString()};
        }

        public override CandidateInHistory GetCandidateHistoryInformation(PublicKey input)
        {
            var historyInformation = State.HistoryMap[input.Hex.ToStringValue()];

            return historyInformation ?? new CandidateInHistory
            {
                PublicKey = input.Hex,
                ContinualAppointmentCount = 0,
                MissedTimeSlots = 0,
                ProducedBlocks = 0,
                ReappointmentCount = 0
            };
        }

        public override FriendlyString GetCandidateHistoryInfoToFriendlyString(PublicKey input)
        {
            return new FriendlyString {Value = GetCandidateHistoryInformation(input).ToString()};
        }

        public override CandidateInHistoryDictionary GetCandidatesHistoryInfo(Empty input)
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

        public override FriendlyString GetCandidatesHistoryInfoToFriendlyString(Empty input)
        {
            return new FriendlyString {Value = GetCandidatesHistoryInfo(input).ToString()};
        }

        public override CandidateInHistoryDictionary GetPageableCandidatesHistoryInfo(PageInfo input)
        {
            var startIndex = input.Start;
            var length = input.Length;
            var result = new CandidateInHistoryDictionary();

            var candidates = State.CandidatesField.Value;
            result.CandidatesNumber = candidates.PublicKeys.Count;

            var take = Math.Min(result.CandidatesNumber - startIndex, length);
            foreach (var candidate in candidates.PublicKeys.Skip(startIndex).Take(take))
            {
                var historyInformation = State.HistoryMap[candidate.ToStringValue()];
                if (historyInformation == null)
                {
                    result.Maps.Add(candidate, new CandidateInHistory());
                    return result;
                }

                var tickets = State.TicketsMap[candidate.ToStringValue()] ?? new Tickets
                {
                    PublicKey = candidate,
                    ObtainedTickets = 0
                };

                historyInformation.CurrentVotesNumber = tickets.ObtainedTickets;
                result.Maps.Add(candidate, historyInformation);
            }

            return result;
        }

        public override FriendlyString GetPageableCandidatesHistoryInfoToFriendlyString(PageInfo input)
        {
            return new FriendlyString {Value = GetPageableCandidatesHistoryInfo(input).ToString()};
        }

        public override MinerList GetCurrentMiners(Empty input)
        {
            var currentTermNumber = State.CurrentTermNumberField.Value;
            if (currentTermNumber == 0)
            {
                currentTermNumber = 1;
            }

            var currentMiners = State.MinersMap[currentTermNumber.ToInt64Value()];

            if (currentMiners == null)
                return null;
            var minerList = new MinerList
            {
                TermNumber = currentMiners.TermNumber
            };
            minerList.Addresses.AddRange(currentMiners.Addresses);
            minerList.PublicKeys.AddRange(currentMiners.PublicKeys);
            return minerList;
        }

        public override FriendlyString GetCurrentMinersToFriendlyString(Empty input)
        {
            return new FriendlyString {Value = GetCurrentMiners(input).ToString()};
        }

        // TODO: Add an API to get unexpired tickets info.
        public override Tickets GetTicketsInformation(PublicKey input)
        {
            var tickets = State.TicketsMap[input.Hex.ToStringValue()];

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

            tickets.VotingRecordsCount = tickets.VotingRecords.Count;
            return tickets;
        }

        public override FriendlyString GetTicketsInformationToFriendlyString(PublicKey input)
        {
            return new FriendlyString {Value = GetTicketsInformation(input).ToString()};
        }

        public override VotingRecord GetVotingRecord(Hash input)
        {
            var txId = input;
            return State.VotingRecordsMap[txId];
        }

        public override SInt64Value QueryObtainedNotExpiredVotes(PublicKey input)
        {
            var tickets = GetTicketsInformation(input);
            if (!tickets.VotingRecords.Any())
            {
                return new SInt64Value();
            }

            return new SInt64Value
            {
                Value = tickets.VotingRecords
                    .Where(vr => vr.To == input.Hex && !vr.IsExpired(State.AgeField.Value))
                    .Aggregate<VotingRecord, long>(0, (current, ticket) => current + ticket.Count)
            };
        }

        public override SInt64Value QueryObtainedVotes(PublicKey input)
        {
            var tickets = GetTicketsInformation(input);
            if (tickets.VotingRecords.Any())
            {
                return new SInt64Value {Value = tickets.ObtainedTickets};
            }

            return new SInt64Value();
        }

        public override Tickets GetPageableTicketsInfo(PageableTicketsInfoInput input)
        {
            var startIndex = input.Start;
            var length = input.Length;
            var publicKey = new PublicKey {Hex = input.PublicKey};
            var tickets = GetTicketsInformation(publicKey);

            var count = tickets.VotingRecords.Count;
            var take = Math.Min(count - startIndex, length);

            var result = new Tickets
            {
                VotingRecords = {tickets.VotingRecords.Skip(startIndex).Take(take)},
                ObtainedTickets = tickets.ObtainedTickets,
                VotedTickets = tickets.VotedTickets,
                HistoryObtainedTickets = tickets.HistoryObtainedTickets,
                HistoryVotedTickets = tickets.HistoryVotedTickets,
                VotingRecordsCount = count,
                VoteToTransactions = {tickets.VoteToTransactions},
                VoteFromTransactions = {tickets.VoteFromTransactions}
            };

            return result;
        }

        public override FriendlyString GetPageableTicketsInfoToFriendlyString(PageableTicketsInfoInput input)
        {
            return new FriendlyString {Value = GetPageableTicketsInfo(input).ToString()};
        }

        public override Tickets GetPageableNotWithdrawnTicketsInfo(PageableTicketsInfoInput input)
        {
            var publicKey = new PublicKey {Hex = input.PublicKey};
            var startIndex = input.Start;
            var length = input.Length;
            var tickets = GetTicketsInformation(publicKey);

            var notWithdrawnVotingRecords = tickets.VotingRecords.Where(vr => !vr.IsWithdrawn).ToList();
            var count = notWithdrawnVotingRecords.Count;
            var take = Math.Min(count - startIndex, length);

            var result = new Tickets
            {
                VotingRecords = {notWithdrawnVotingRecords.Skip(startIndex).Take(take)},
                ObtainedTickets = tickets.ObtainedTickets,
                VotedTickets = tickets.VotedTickets,
                HistoryObtainedTickets = tickets.HistoryObtainedTickets,
                HistoryVotedTickets = tickets.HistoryVotedTickets,
                VotingRecordsCount = count,
                VoteToTransactions = {tickets.VoteToTransactions},
                VoteFromTransactions = {tickets.VoteFromTransactions}
            };

            return result;
        }

        public override FriendlyString GetPageableNotWithdrawnTicketsInfoToFriendlyString(PageableTicketsInfoInput input)
        {
            return new FriendlyString {Value = GetPageableNotWithdrawnTicketsInfo(input).ToString()};
        }

        public override TicketsHistories GetPageableTicketsHistories(PageableTicketsInfoInput input)
        {
            var publicKey = new PublicKey {Hex = input.PublicKey};
            var startIndex = input.Start;
            var length = input.Length;
            var histories = new TicketsHistories();
            var result = new TicketsHistories();

            var tickets = GetTicketsInformation(publicKey);

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

            var take = Math.Min(histories.Values.Count - startIndex, length);
            result.Values.AddRange(histories.Values.Skip(startIndex).Take(take));
            result.HistoriesNumber = histories.Values.Count;

            return result;
        }

        public override FriendlyString GetPageableTicketsHistoriesToFriendlyString(PageableTicketsInfoInput input)
        {
            return new FriendlyString {Value = GetPageableTicketsHistories(input).ToString()};
        }

        /// <summary>
        /// Order by:
        /// 0 - Announcement order. (Default)
        /// 1 - Obtained votes ascending.
        /// 2 - Obtained votes descending.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override TicketsDictionary GetPageableElectionInfo(PageableElectionInfoInput input)
        {
            var startIndex = input.Start;
            var length = input.Length;
            var orderBy = input.OrderBy;
            
            var publicKeys = State.CandidatesField.Value.PublicKeys;
            length = length == 0 ? publicKeys.Count : length;
            
            var dict = new Dictionary<string, Tickets>();
            var take = Math.Min(publicKeys.Count - startIndex, length);
            foreach (var publicKey in publicKeys.Skip(startIndex).Take(take))
            {
                var tickets = State.TicketsMap[publicKey.ToStringValue()];
                if (tickets != null)
                {
                    dict.Add(publicKey, tickets);
                }
            }
            
            if (orderBy == 0)
                return new TicketsDictionary
                {
                    Maps = {dict}
                };

            if (orderBy == 1)
                return new TicketsDictionary
                {
                    Maps =
                    {
                        dict.OrderBy(p => p.Value.ObtainedTickets).Skip(startIndex).Take(take)
                            .ToDictionary(p => p.Key, p => p.Value)
                    }
                };

            if (orderBy == 2)
                return new TicketsDictionary
                {
                    Maps =
                    {
                        dict.OrderByDescending(p => p.Value.ObtainedTickets).Skip(startIndex).Take(take)
                            .ToDictionary(p => p.Key, p => p.Value)
                    }
                };

            return new TicketsDictionary();
        }

        public override FriendlyString GetPageableElectionInfoToFriendlyString(PageableElectionInfoInput input)
        {
            return new FriendlyString {Value = GetPageableElectionInfo(input).ToString()};
        }

        public override SInt64Value GetBlockchainAge(Empty input)
        {
            return new SInt64Value {Value = State.AgeField.Value};
        }

        public override StringList GetCurrentVictories(Empty input)
        {
            return TryToGetVictories(out var victories)
                ? new StringList {Values = {victories.PublicKeys}}
                : new StringList();
        }

        public override FriendlyString GetCurrentVictoriesToFriendlyString(Empty input)
        {
            return new FriendlyString {Value = GetCurrentVictories(input).ToString()};
        }

        public override TermSnapshot GetTermSnapshot(SInt64Value input)
        {
            return State.SnapshotMap[input.Value.ToInt64Value()];
        }

        public override FriendlyString GetTermSnapshotToFriendlyString(SInt64Value input)
        {
            return new FriendlyString {Value = GetTermSnapshot(input).ToString()};
        }

        public override Alias QueryAlias(PublicKey input)
        {
            var aliasString = State.AliasesMap[input.Hex.ToStringValue()]?.Value;
            var alias = aliasString ?? input.Hex.Substring(0, DPoSContractConsts.AliasLimit);
            return new Alias {Value = alias};
        }

        public override SInt64Value GetTermNumberByRoundNumber(SInt64Value input)
        {
            var map = State.TermNumberLookupField.Value.Map;
            Assert(map != null, "Term number not found.");
            var roundNumber = map?.OrderBy(p => p.Key).Last(p => input.Value >= p.Value).Key ?? (long) 0;
            return new SInt64Value {Value = roundNumber};
        }

        public override SInt64Value GetVotesCount(Empty input)
        {
            return new SInt64Value {Value = State.VotesCountField.Value};
        }

        public override SInt64Value GetTicketsCount(Empty input)
        {
            return new SInt64Value {Value = State.TicketsCountField.Value};
        }

        public override SInt64Value QueryCurrentDividendsForVoters(Empty input)
        {
            return new SInt64Value {Value = (long) (QueryCurrentDividends(input).Value * DPoSContractConsts.VotersRatio)};
        }

        public override SInt64Value QueryCurrentDividends(Empty input)
        {
            var currentRoundNumber = GetCurrentRoundNumber(input);
            if (currentRoundNumber.Value == 0)
            {
                return currentRoundNumber;
            }

            var round = State.RoundsMap[currentRoundNumber.Value.ToInt64Value()];
            if (round == null)
            {
                return new SInt64Value();
            }

            var minedBlocks = round.GetMinedBlocks();
            return new SInt64Value {Value = minedBlocks * DPoSContractConsts.ElfTokenPerBlock};
        }

        public override StringList QueryAliasesInUse(Empty input)
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

        public override SInt64Value QueryMinedBlockCountInCurrentTerm(PublicKey input)
        {
            var round = State.RoundsMap[GetCurrentRoundNumber(new Empty()).Value.ToInt64Value()];
            if (round != null)
            {
                if (round.RealTimeMinersInformation.ContainsKey(input.Hex))
                {
                    return new SInt64Value {Value = round.RealTimeMinersInformation[input.Hex].ProducedBlocks};
                }
            }

            return new SInt64Value();
        }

        public override FriendlyString QueryAliasesInUseToFriendlyString(Empty input)
        {
            return new FriendlyString {Value = QueryAliasesInUse(input).ToString()};
        }
    }
}