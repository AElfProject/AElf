using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.DPoS
{
    public class Election
    {
        private ulong CurrentAge => _dataStructures.AgeField.GetValue();

        private readonly DataStructures _dataStructures;

        public Election(DataStructures dataStructures)
        {
            _dataStructures = dataStructures;
        }

        public ActionResult AnnounceElection(string alias = "")
        {
            var publicKey = Api.RecoverPublicKey().ToHex();
            // A voter cannot join the election before all his voting record expired.
            if (_dataStructures.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
            {
                foreach (var voteToTransaction in tickets.VoteToTransactions)
                {
                    if (_dataStructures.VotingRecordsMap.TryGet(voteToTransaction, out var votingRecord))
                    {
                        Api.Assert(votingRecord.IsWithdrawn, GlobalConfig.VoterCannotAnnounceElection);
                    }
                }
            }

            Api.LockToken(GlobalConfig.LockTokenForElection);
            var candidates = _dataStructures.CandidatesField.GetValue();
            if (!candidates.PublicKeys.Contains(publicKey))
            {
                candidates.PublicKeys.Add(publicKey);
            }

            _dataStructures.CandidatesField.SetValue(candidates);

            if (alias == "" || alias.Length > GlobalConfig.AliasLimit)
            {
                alias = publicKey.Substring(0, GlobalConfig.AliasLimit);
            }

            if (_dataStructures.AliasesLookupMap.TryGet(alias.ToStringValue(), out var publicKeyOfThisAlias) &&
                publicKey == publicKeyOfThisAlias.Value)
            {
                return new ActionResult {Success = true};
            }

            _dataStructures.AliasesLookupMap.SetValue(alias.ToStringValue(), publicKey.ToStringValue());
            _dataStructures.AliasesMap.SetValue(publicKey.ToStringValue(), alias.ToStringValue());

            // Add this alias to history information of this candidate.
            if (_dataStructures.HistoryMap.TryGet(publicKey.ToStringValue(), out var candidateHistoryInformation))
            {
                if (!candidateHistoryInformation.Aliases.Contains(alias))
                {
                    candidateHistoryInformation.Aliases.Add(alias);
                    candidateHistoryInformation.CurrentAlias = alias;
                }

                _dataStructures.HistoryMap.SetValue(publicKey.ToStringValue(), candidateHistoryInformation);
            }
            else
            {
                _dataStructures.HistoryMap.SetValue(publicKey.ToStringValue(), new CandidateInHistory
                {
                    CurrentAlias = alias
                });
            }

            return new ActionResult {Success = true};
        }

        public ActionResult QuitElection()
        {
            Api.UnlockToken(Api.GetFromAddress(), GlobalConfig.LockTokenForElection);
            var candidates = _dataStructures.CandidatesField.GetValue();
            candidates.PublicKeys.Remove(Api.RecoverPublicKey().ToHex());
            _dataStructures.CandidatesField.SetValue(candidates);

            return new ActionResult {Success = true};
        }

        public ActionResult Vote(string candidatePublicKey, ulong amount, int lockTime)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            //TODO: Recover after testing.
//            Api.Assert(lockTime.InRange(90, 1095), GlobalConfig.LockDayIllegal);

            // Cannot vote to non-candidate.
            var candidates = _dataStructures.CandidatesField.GetValue();
            Api.Assert(candidates.PublicKeys.Contains(candidatePublicKey),
                GlobalConfig.TargetNotAnnounceElection);

            // A candidate cannot vote to anybody.
            Api.Assert(!candidates.PublicKeys.Contains(Api.RecoverPublicKey().ToHex()),
                GlobalConfig.CandidateCannotVote);

            // Transfer the tokens to Consensus Contract address.
            Api.LockToken(amount);

            var currentTermNumber = _dataStructures.CurrentTermNumberField.GetValue();
            var currentRoundNumber = _dataStructures.CurrentRoundNumberField.GetValue();

            // To make up a VotingRecord instance.
            var blockchainStartTimestamp = _dataStructures.BlockchainStartTimestamp.GetValue();
            var votingRecord = new VotingRecord
            {
                Count = amount,
                From = Api.RecoverPublicKey().ToHex(),
                To = candidatePublicKey,
                RoundNumber = currentRoundNumber,
                TransactionId = Api.GetTxnHash(),
                VoteAge = CurrentAge,
                UnlockAge = CurrentAge + (ulong) lockTime,
                TermNumber = currentTermNumber,
                VoteTimestamp = blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge).ToTimestamp(),
                UnlockTimestamp = blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge + (ulong) lockTime)
                    .ToTimestamp()
            };
            votingRecord.LockDaysList.Add(lockTime);

            // Add the transaction id of this voting record to the tickets information of the voter.
            if (_dataStructures.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
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
            _dataStructures.TicketsMap.SetValue(Api.RecoverPublicKey().ToHex().ToStringValue(), tickets);

            // Add the transaction id of this voting record to the tickets information of the candidate.
            if (_dataStructures.TicketsMap.TryGet(candidatePublicKey.ToStringValue(), out var candidateTickets))
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
            _dataStructures.TicketsMap.SetValue(candidatePublicKey.ToStringValue(), candidateTickets);

            // Update the amount of votes (voting records of whole system).
            var currentCount = _dataStructures.VotesCountField.GetValue();
            currentCount += 1;
            _dataStructures.VotesCountField.SetValue(currentCount);

            // Update the amount of tickets.
            var ticketsCount = _dataStructures.TicketsCountField.GetValue();
            ticketsCount += votingRecord.Count;
            _dataStructures.TicketsCountField.SetValue(ticketsCount);

            // Add this voting record to voting records map.
            _dataStructures.VotingRecordsMap.SetValue(votingRecord.TransactionId, votingRecord);

            // Tell Dividends Contract to add weights for this voting record.
            Api.SendInline(Api.DividendsContractAddress, "AddWeights", votingRecord.Weight, currentTermNumber + 1);

            Console.WriteLine($"Weights of vote {votingRecord.TransactionId.ToHex()}: {votingRecord.Weight}");
            Console.WriteLine($"Vote duration: {stopwatch.ElapsedMilliseconds} ms.");
            return new ActionResult {Success = true};
        }

        public ActionResult ReceiveDividends(string transactionId)
        {
            if (_dataStructures.VotingRecordsMap.TryGet(Hash.LoadHex(transactionId), out var votingRecord) &&
                votingRecord.From == Api.RecoverPublicKey().ToHex())
            {
                Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord);
                return new ActionResult {Success = true};
            }

            return new ActionResult {Success = false, ErrorMessage = "Voting record not found."};
        }

        public ActionResult ReceiveDividends()
        {
            if (_dataStructures.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                if (!tickets.VoteToTransactions.Any())
                {
                    return new ActionResult {Success = false, ErrorMessage = "Voting records not found."};
                }

                foreach (var transactionId in tickets.VoteToTransactions)
                {
                    if (_dataStructures.VotingRecordsMap.TryGet(transactionId, out var votingRecord))
                    {
                        Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord);
                    }
                }
            }

            return new ActionResult {Success = true};
        }

        public ActionResult Withdraw(string transactionId, bool withoutLimitation)
        {
            if (_dataStructures.VotingRecordsMap.TryGet(Hash.LoadHex(transactionId), out var votingRecord))
            {
                if (votingRecord.IsWithdrawn)
                {
                    return new ActionResult
                        {Success = false, ErrorMessage = "This voting record has already withdrawn."};
                }

                if ((votingRecord.UnlockAge <= CurrentAge || withoutLimitation) && votingRecord.IsWithdrawn == false)
                {
                    Api.SendInline(Api.TokenContractAddress, "Transfer", Api.GetFromAddress(), votingRecord.Count);
                    Api.SendInline(Api.DividendsContractAddress, "SubWeights", votingRecord.Weight,
                        _dataStructures.CurrentTermNumberField.GetValue());

                    var blockchainStartTimestamp = _dataStructures.BlockchainStartTimestamp.GetValue();
                    votingRecord.WithdrawTimestamp =
                        blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge).ToTimestamp();
                    votingRecord.IsWithdrawn = true;

                    _dataStructures.VotingRecordsMap.SetValue(Hash.LoadHex(transactionId), votingRecord);

                    var ticketsCount = _dataStructures.TicketsCountField.GetValue();
                    ticketsCount -= votingRecord.Count;
                    _dataStructures.TicketsCountField.SetValue(ticketsCount);

                    if (_dataStructures.TicketsMap.TryGet(votingRecord.From.ToStringValue(), out var ticketsOfVoter))
                    {
                        ticketsOfVoter.VotedTickets -= votingRecord.Count;
                        _dataStructures.TicketsMap.SetValue(votingRecord.From.ToStringValue(), ticketsOfVoter);
                    }

                    if (_dataStructures.TicketsMap.TryGet(votingRecord.To.ToStringValue(), out var ticketsOfCandidate))
                    {
                        ticketsOfCandidate.ObtainedTickets -= votingRecord.Count;
                        _dataStructures.TicketsMap.SetValue(votingRecord.To.ToStringValue(), ticketsOfCandidate);
                    }
                }
            }
            else
            {
                return new ActionResult {Success = false, ErrorMessage = "Voting record not found."};
            }

            return new ActionResult {Success = true};
        }

        public ActionResult Withdraw(bool withoutLimitation = false)
        {
            var voterPublicKey = Api.RecoverPublicKey().ToHex();
            var ticketsCount = _dataStructures.TicketsCountField.GetValue();
            var withdrawnAmount = 0UL;
            var candidatesVotesDict = new Dictionary<string, ulong>();

            if (_dataStructures.TicketsMap.TryGet(voterPublicKey.ToStringValue(), out var tickets))
            {
                foreach (var transactionId in tickets.VoteToTransactions)
                {
                    if (_dataStructures.VotingRecordsMap.TryGet(transactionId, out var votingRecord))
                    {
                        if (votingRecord.UnlockAge > CurrentAge && !withoutLimitation)
                        {
                            continue;
                        }
                        
                        Api.SendInline(Api.TokenContractAddress, "Transfer", Api.GetFromAddress(), votingRecord.Count);
                        Api.SendInline(Api.DividendsContractAddress, "SubWeights", votingRecord.Weight,
                            _dataStructures.CurrentTermNumberField.GetValue());

                        var blockchainStartTimestamp = _dataStructures.BlockchainStartTimestamp.GetValue();
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

                        _dataStructures.VotingRecordsMap.SetValue(votingRecord.TransactionId, votingRecord);
                    }
                }

                ticketsCount -= withdrawnAmount;
                _dataStructures.TicketsCountField.SetValue(ticketsCount);

                tickets.VotedTickets -= withdrawnAmount;
                _dataStructures.TicketsMap.SetValue(voterPublicKey.ToStringValue(), tickets);

                foreach (var candidateVote in candidatesVotesDict)
                {
                    if (_dataStructures.TicketsMap.TryGet(candidateVote.Key.ToStringValue(), out var ticketsOfCandidate))
                    {
                        ticketsOfCandidate.ObtainedTickets -= candidateVote.Value;
                        _dataStructures.TicketsMap.SetValue(candidateVote.Key.ToStringValue(), ticketsOfCandidate);
                    }
                }
            }

            return new ActionResult {Success = true};
        }
    }
}