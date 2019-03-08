using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS.Extensions;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract
    {
        private ulong CurrentAge => State.AgeField.Value;

        public ActionResult AnnounceElection(string alias = "")
        {
            var publicKey = Context.RecoverPublicKey().ToHex();
            // A voter cannot join the election before all his voting record expired.
            var tickets = State.TicketsMap[publicKey.ToStringValue()];
            if (tickets!=null)
            {
                foreach (var voteToTransaction in tickets.VoteToTransactions)
                {
                    var votingRecord = State.VotingRecordsMap[voteToTransaction];
                    if (votingRecord!=null)
                    {
                        Assert(votingRecord.IsWithdrawn, DPoSContractConsts.VoterCannotAnnounceElection);
                    }
                }
            }

            State.TokenContract.Lock(Context.Sender, DPoSContractConsts.LockTokenForElection);
            var candidates = State.CandidatesField.Value;
            if (!candidates.PublicKeys.Contains(publicKey))
            {
                candidates.PublicKeys.Add(publicKey);
            }

            State.CandidatesField.Value = candidates;

            if (alias == "" || alias.Length > DPoSContractConsts.AliasLimit)
            {
                alias = publicKey.Substring(0, DPoSContractConsts.AliasLimit);
            }

            var publicKeyOfThisAlias = State.AliasesLookupMap[alias.ToStringValue()];
            if (publicKeyOfThisAlias!=null &&
                publicKey == publicKeyOfThisAlias.Value)
            {
                return new ActionResult {Success = true};
            }

            State.AliasesLookupMap[alias.ToStringValue()] = publicKey.ToStringValue();
            State.AliasesMap[publicKey.ToStringValue()] = alias.ToStringValue();

            // Add this alias to history information of this candidate.
            var candidateHistoryInformation = State.HistoryMap[publicKey.ToStringValue()];
            if (candidateHistoryInformation!=null)
            {
                if (!candidateHistoryInformation.Aliases.Contains(alias))
                {
                    candidateHistoryInformation.Aliases.Add(alias);
                    candidateHistoryInformation.CurrentAlias = alias;
                }

                State.HistoryMap[publicKey.ToStringValue()] = candidateHistoryInformation;
            }
            else
            {
                State.HistoryMap[publicKey.ToStringValue()] = new CandidateInHistory
                {
                    CurrentAlias = alias
                };
            }

            return new ActionResult {Success = true};
        }

        public ActionResult QuitElection()
        {
            State.TokenContract.Unlock(Context.Sender, DPoSContractConsts.LockTokenForElection);
            var candidates = State.CandidatesField.Value;
            candidates.PublicKeys.Remove(Context.RecoverPublicKey().ToHex());
            State.CandidatesField.Value = candidates;

            return new ActionResult {Success = true};
        }

        public ActionResult Vote(string candidatePublicKey, ulong amount, int lockTime)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Assert(lockTime.InRange(90, 1095), DPoSContractConsts.LockDayIllegal);

            // Cannot vote to non-candidate.
            var candidates = State.CandidatesField.Value;
            Assert(candidates.PublicKeys.Contains(candidatePublicKey),
                DPoSContractConsts.TargetNotAnnounceElection);

            // A candidate cannot vote to anybody.
            Assert(!candidates.PublicKeys.Contains(Context.RecoverPublicKey().ToHex()),
                DPoSContractConsts.CandidateCannotVote);

            // Transfer the tokens to Consensus Contract address.
            State.TokenContract.Lock(Context.Sender, amount);

            var currentTermNumber = State.CurrentTermNumberField.Value;
            var currentRoundNumber = State.CurrentRoundNumberField.Value;

            // To make up a VotingRecord instance.
            var blockchainStartTimestamp = State.BlockchainStartTimestamp.Value;
            var votingRecord = new VotingRecord
            {
                Count = amount,
                From = Context.RecoverPublicKey().ToHex(),
                To = candidatePublicKey,
                RoundNumber = currentRoundNumber,
                TransactionId = Context.TransactionId,
                VoteAge = CurrentAge,
                UnlockAge = CurrentAge + (ulong) lockTime,
                TermNumber = currentTermNumber,
                VoteTimestamp = blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge).ToTimestamp(),
                UnlockTimestamp = blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge + (ulong) lockTime)
                    .ToTimestamp()
            };
            votingRecord.LockDaysList.Add(lockTime);

            // Add the transaction id of this voting record to the tickets information of the voter.
            var tickets = State.TicketsMap[Context.RecoverPublicKey().ToHex().ToStringValue()];
            if (tickets != null)
            {
                tickets.VoteToTransactions.Add(votingRecord.TransactionId);
            }
            else
            {
                tickets = new Tickets();
                tickets.VoteToTransactions.Add(votingRecord.TransactionId);
            }

            tickets.VotedTickets += votingRecord.Count;
            tickets.HistoryVotedTickets += votingRecord.Count;
            State.TicketsMap[Context.RecoverPublicKey().ToHex().ToStringValue()] = tickets;

            // Add the transaction id of this voting record to the tickets information of the candidate.
            var candidateTickets = State.TicketsMap[candidatePublicKey.ToStringValue()];
            if (candidateTickets != null)
            {
                candidateTickets.VoteFromTransactions.Add(votingRecord.TransactionId);
            }
            else
            {
                candidateTickets = new Tickets();
                candidateTickets.VoteFromTransactions.Add(votingRecord.TransactionId);
            }

            candidateTickets.ObtainedTickets += votingRecord.Count;
            candidateTickets.HistoryObtainedTickets += votingRecord.Count;
            State.TicketsMap[candidatePublicKey.ToStringValue()] = candidateTickets;

            // Update the amount of votes (voting records of whole system).
            var currentCount = State.VotesCountField.Value;
            currentCount += 1;
            State.VotesCountField.Value = currentCount;

            // Update the amount of tickets.
            var ticketsCount = State.TicketsCountField.Value;
            ticketsCount += votingRecord.Count;
            State.TicketsCountField.Value = ticketsCount;

            // Add this voting record to voting records map.
            State.VotingRecordsMap[votingRecord.TransactionId] = votingRecord;

            // Tell Dividends Contract to add weights for this voting record.
            State.DividendContract.AddWeights(votingRecord.Weight, currentTermNumber + 1);

            Context.LogDebug(() => $"Weights of vote {votingRecord.TransactionId.ToHex()}: {votingRecord.Weight}");
            Context.LogDebug(() => $"Vote duration: {stopwatch.ElapsedMilliseconds} ms.");

            return new ActionResult {Success = true};
        }

        public ActionResult ReceiveDividendsByTransactionId(string transactionId)
        {
            var votingRecord = State.VotingRecordsMap[Hash.LoadHex(transactionId)];
            if (votingRecord != null &&
                votingRecord.From == Context.RecoverPublicKey().ToHex())
            {
                State.DividendContract.TransferDividends(votingRecord);
                return new ActionResult {Success = true};
            }

            return new ActionResult {Success = false, ErrorMessage = "Voting record not found."};
        }

        public ActionResult ReceiveAllDividends()
        {
            var tickets = State.TicketsMap[Context.RecoverPublicKey().ToHex().ToStringValue()];
            if (tickets != null)
            {
                if (!tickets.VoteToTransactions.Any())
                {
                    return new ActionResult {Success = false, ErrorMessage = "Voting records not found."};
                }

                foreach (var transactionId in tickets.VoteToTransactions)
                {
                    var votingRecord = State.VotingRecordsMap[transactionId];
                    if (votingRecord != null)
                    {
                        State.DividendContract.TransferDividends(votingRecord);
                    }
                }
            }

            return new ActionResult {Success = true};
        }

        public ActionResult WithdrawByTransactionId(string transactionId, bool withoutLimitation)
        {
            var votingRecord = State.VotingRecordsMap[Hash.LoadHex(transactionId)];
            if (votingRecord != null)
            {
                if (votingRecord.IsWithdrawn)
                {
                    return new ActionResult
                        {Success = false, ErrorMessage = "This voting record has already withdrawn."};
                }

                if ((votingRecord.UnlockAge <= CurrentAge || withoutLimitation) && votingRecord.IsWithdrawn == false)
                {
                    State.TokenContract.Transfer(Context.Sender, votingRecord.Count);
                    State.DividendContract.SubWeights(votingRecord.Weight, State.CurrentTermNumberField.Value);

                    var blockchainStartTimestamp = State.BlockchainStartTimestamp.Value;
                    votingRecord.WithdrawTimestamp =
                        blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge).ToTimestamp();
                    votingRecord.IsWithdrawn = true;

                    State.VotingRecordsMap[Hash.LoadHex(transactionId)] = votingRecord;

                    var ticketsCount = State.TicketsCountField.Value;
                    ticketsCount -= votingRecord.Count;
                    State.TicketsCountField.Value = ticketsCount;

                    var ticketsOfVoter = State.TicketsMap[votingRecord.From.ToStringValue()];
                    if (ticketsOfVoter != null)
                    {
                        ticketsOfVoter.VotedTickets -= votingRecord.Count;
                        State.TicketsMap[votingRecord.From.ToStringValue()] = ticketsOfVoter;
                    }

                    var ticketsOfCandidate = State.TicketsMap[votingRecord.To.ToStringValue()];
                    if (ticketsOfCandidate != null)
                    {
                        ticketsOfCandidate.ObtainedTickets -= votingRecord.Count;
                        State.TicketsMap[votingRecord.To.ToStringValue()] = ticketsOfCandidate;
                    }
                }
            }
            else
            {
                return new ActionResult {Success = false, ErrorMessage = "Voting record not found."};
            }

            return new ActionResult {Success = true};
        }

        public ActionResult WithdrawAll(bool withoutLimitation = false)
        {
            var voterPublicKey = Context.RecoverPublicKey().ToHex();
            var ticketsCount = State.TicketsCountField.Value;
            var withdrawnAmount = 0UL;
            var candidatesVotesDict = new Dictionary<string, ulong>();

            var tickets = State.TicketsMap[voterPublicKey.ToStringValue()];
            if (tickets != null)
            {
                foreach (var transactionId in tickets.VoteToTransactions)
                {
                    var votingRecord = State.VotingRecordsMap[transactionId];
                    if (votingRecord != null)
                    {
                        if (votingRecord.UnlockAge > CurrentAge && !withoutLimitation)
                        {
                            continue;
                        }

                        State.TokenContract.Transfer(Context.Sender, votingRecord.Count);
                        State.DividendContract.SubWeights(votingRecord.Weight, State.CurrentTermNumberField.Value);

                        var blockchainStartTimestamp = State.BlockchainStartTimestamp.Value;
                        votingRecord.WithdrawTimestamp =
                            blockchainStartTimestamp.ToDateTime().AddMinutes(CurrentAge).ToTimestamp();
                        votingRecord.IsWithdrawn = true;

                        withdrawnAmount += votingRecord.Count;
                        if (candidatesVotesDict.ContainsKey(votingRecord.To))
                        {
                            candidatesVotesDict[votingRecord.To] += votingRecord.Count;
                        }
                        else
                        {
                            candidatesVotesDict.Add(votingRecord.To, votingRecord.Count);
                        }

                        State.VotingRecordsMap[votingRecord.TransactionId] = votingRecord;
                    }
                }

                ticketsCount -= withdrawnAmount;
                State.TicketsCountField.Value = ticketsCount;

                tickets.VotedTickets -= withdrawnAmount;
                State.TicketsMap[voterPublicKey.ToStringValue()] = tickets;

                foreach (var candidateVote in candidatesVotesDict)
                {
                    var ticketsOfCandidate = State.TicketsMap[candidateVote.Key.ToStringValue()];
                    if (ticketsOfCandidate != null)
                    {
                        ticketsOfCandidate.ObtainedTickets -= candidateVote.Value;
                        State.TicketsMap[candidateVote.Key.ToStringValue()] = ticketsOfCandidate;
                    }
                }
            }

            return new ActionResult {Success = true};
        }
    }
}