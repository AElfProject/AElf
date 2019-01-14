using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus.Contracts
{
    public class Election
    {
        private ulong CurrentAge => _collection.AgeField.GetValue();

        private readonly DataCollection _collection;

        public Election(DataCollection collection)
        {
            _collection = collection;
        }

        public ActionResult AnnounceElection(string alias = "")
        {
            var publicKey = Api.RecoverPublicKey().ToHex();
            // A voter cannot join the election before all his voting record expired.
            if (_collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
            {
                Api.Assert(
                    !tickets.VotingRecords.Any(
                        t => !t.IsExpired(_collection.AgeField.GetValue()) && t.From == publicKey),
                    GlobalConfig.VoterCannotAnnounceElection);
            }

            Api.LockToken(GlobalConfig.LockTokenForElection);
            var candidates = _collection.CandidatesField.GetValue();
            if (!candidates.PublicKeys.Contains(publicKey))
            {
                candidates.PublicKeys.Add(publicKey);
            }

            _collection.CandidatesField.SetValue(candidates);

            if (alias == "" || alias.Length > GlobalConfig.AliasLimit)
            {
                alias = publicKey.Substring(0, GlobalConfig.AliasLimit);
            }

            if (_collection.AliasesLookupMap.TryGet(alias.ToStringValue(), out var publicKeyOfThisAlias) &&
                publicKey == publicKeyOfThisAlias.Value)
            {
                return new ActionResult {Success = true};
            }

            _collection.AliasesLookupMap.SetValue(alias.ToStringValue(), publicKey.ToStringValue());
            _collection.AliasesMap.SetValue(publicKey.ToStringValue(), alias.ToStringValue());

            // Add this alias to history information of this candidate.
            if (_collection.HistoryMap.TryGet(publicKey.ToStringValue(), out var candidateHistoryInformation))
            {
                if (!candidateHistoryInformation.Aliases.Contains(alias))
                {
                    candidateHistoryInformation.Aliases.Add(alias);
                    candidateHistoryInformation.CurrentAlias = alias;
                }

                _collection.HistoryMap.SetValue(publicKey.ToStringValue(), candidateHistoryInformation);
            }
            else
            {
                _collection.HistoryMap.SetValue(publicKey.ToStringValue(), new CandidateInHistory
                {
                    CurrentAlias = alias
                });
            }

            return new ActionResult {Success = true};
        }

        public ActionResult QuitElection()
        {
            Api.UnlockToken(Api.GetFromAddress(), GlobalConfig.LockTokenForElection);
            var candidates = _collection.CandidatesField.GetValue();
            candidates.PublicKeys.Remove(Api.RecoverPublicKey().ToHex());
            _collection.CandidatesField.SetValue(candidates);

            return new ActionResult {Success = true};
        }

        public ActionResult Vote(string candidatePublicKey, ulong amount, int lockTime)
        {
            //TODO: Recover after testing.
//            Api.Assert(lockTime.InRange(90, 1095), GlobalConfig.LockDayIllegal);

            Api.Assert(_collection.CandidatesField.GetValue().PublicKeys.Contains(candidatePublicKey),
                GlobalConfig.TargetNotAnnounceElection);

            Api.Assert(!_collection.CandidatesField.GetValue().PublicKeys.Contains(Api.RecoverPublicKey().ToHex()),
                GlobalConfig.CandidateCannotVote);

            Api.LockToken(amount);

            var blockchainStartTimestamp = _collection.BlockchainStartTimestamp.GetValue();
            var votingRecord = new VotingRecord
            {
                Count = amount,
                From = Api.RecoverPublicKey().ToHex(),
                To = candidatePublicKey,
                RoundNumber = _collection.CurrentRoundNumberField.GetValue(),
                TransactionId = Api.GetTxnHash(),
                VoteAge = CurrentAge,
                UnlockAge = CurrentAge + (ulong) lockTime,
                TermNumber = _collection.CurrentTermNumberField.GetValue(),
                VoteTimestamp = blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge).ToTimestamp(),
                UnlockTimestamp = blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge + (ulong) lockTime)
                    .ToTimestamp()
            };
            votingRecord.LockDaysList.Add(lockTime);

            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                tickets.VotingRecords.Add(votingRecord);
            }
            else
            {
                tickets = new Tickets();
                tickets.VotingRecords.Add(votingRecord);
            }

            tickets.VotedTickets += votingRecord.Count;
            tickets.HistoryVotedTickets += votingRecord.Count;
            _collection.TicketsMap.SetValue(Api.RecoverPublicKey().ToHex().ToStringValue(), tickets);

            if (_collection.TicketsMap.TryGet(candidatePublicKey.ToStringValue(), out var candidateTickets))
            {
                candidateTickets.VotingRecords.Add(votingRecord);
            }
            else
            {
                candidateTickets = new Tickets();
                candidateTickets.VotingRecords.Add(votingRecord);
            }

            candidateTickets.ObtainedTickets += votingRecord.Count;
            candidateTickets.HistoryObtainedTickets += votingRecord.Count;
            _collection.TicketsMap.SetValue(candidatePublicKey.ToStringValue(), candidateTickets);

            var currentCount = _collection.VotesCountField.GetValue();
            currentCount += 1;
            _collection.VotesCountField.SetValue(currentCount);

            var ticketsCount = _collection.TicketsCountField.GetValue();
            ticketsCount += votingRecord.Count;
            _collection.TicketsCountField.SetValue(ticketsCount);

            Api.SendInline(Api.DividendsContractAddress, "AddWeights", votingRecord.Weight,
                _collection.CurrentTermNumberField.GetValue());

            return new ActionResult {Success = true};
        }

        public ActionResult ReceiveDividends(string transactionId)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecord =
                    tickets.VotingRecords.FirstOrDefault(vr => vr.TransactionId.ToHex() == transactionId);

                if (votingRecord != null)
                {
                    Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord);
                }
                else
                {
                    return new ActionResult {Success = false, ErrorMessage = "Voting record not found."};
                }
            }

            return new ActionResult {Success = true};
        }

        public ActionResult ReceiveDividends()
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                if (!tickets.VotingRecords.Any())
                {
                    return new ActionResult {Success = false, ErrorMessage = "Voting records not found."};
                }

                foreach (var votingRecord in tickets.VotingRecords)
                {
                    Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord);
                }
            }

            return new ActionResult {Success = true};
        }

        public ActionResult Withdraw(string transactionId, bool withoutLimitation)
        {
            var voterPublicKey = Api.RecoverPublicKey().ToHex();
            var candidatePublicKey = "";
            ulong amount = 0;

            if (_collection.TicketsMap.TryGet(voterPublicKey.ToStringValue(), out var tickets))
            {
                var votingRecord =
                    tickets.VotingRecords.FirstOrDefault(vr => vr.TransactionId.ToHex() == transactionId);

                if (votingRecord != null && votingRecord.IsWithdrawn)
                {
                    return new ActionResult {Success = false, ErrorMessage = "This ticket has already withdrawn."};
                }

                if (votingRecord != null && (votingRecord.UnlockAge >= CurrentAge || withoutLimitation) &&
                    votingRecord.IsWithdrawn == false)
                {
                    amount = votingRecord.Count;

                    candidatePublicKey = votingRecord.To;
                    Api.SendInline(Api.TokenContractAddress, "Transfer", Api.GetFromAddress(), amount);
                    Api.SendInline(Api.DividendsContractAddress, "SubWeights", votingRecord.Weight,
                        _collection.CurrentTermNumberField.GetValue());

                    var blockchainStartTimestamp = _collection.BlockchainStartTimestamp.GetValue();
                    votingRecord.WithdrawTimestamp =
                        blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge).ToTimestamp();
                    votingRecord.IsWithdrawn = true;

                    var ticketsCount = _collection.TicketsCountField.GetValue();
                    ticketsCount -= amount;
                    _collection.TicketsCountField.SetValue(ticketsCount);
                }
            }
            else
            {
                return new ActionResult {Success = false, ErrorMessage = "Tickets information not found."};
            }

            if (candidatePublicKey == "")
            {
                return new ActionResult {Success = false, ErrorMessage = "Tickets information not found."};
            }
            
            if (_collection.TicketsMap.TryGet(candidatePublicKey.ToStringValue(), out var ticketsOfCandidate))
            {
                var votingRecord =
                    ticketsOfCandidate.VotingRecords.FirstOrDefault(vr => vr.TransactionId.ToHex() == transactionId);

                if (votingRecord != null && (votingRecord.UnlockAge >= CurrentAge || withoutLimitation) &&
                    votingRecord.IsWithdrawn == false)
                {
                    var blockchainStartTimestamp = _collection.BlockchainStartTimestamp.GetValue();
                    votingRecord.WithdrawTimestamp =
                        blockchainStartTimestamp.ToDateTime().AddMinutes(CurrentAge).ToTimestamp();
                    votingRecord.IsWithdrawn = true;
                }
            }

            if (ticketsOfCandidate != null)
            {
                tickets.VotedTickets -= amount;
                ticketsOfCandidate.ObtainedTickets -= amount;
                _collection.TicketsMap.SetValue(voterPublicKey.ToStringValue(), tickets);
                _collection.TicketsMap.SetValue(candidatePublicKey.ToStringValue(), ticketsOfCandidate);
            }
            else
            {
                return new ActionResult {Success = false, ErrorMessage = "Tickets information incorrect."};
            }

            return new ActionResult {Success = true};
        }

        public ActionResult Withdraw(bool withoutLimitation = false)
        {
            var voterPublicKey = Api.RecoverPublicKey().ToHex();
            var candidatePublicKeys = new List<string>();
            if (_collection.TicketsMap.TryGet(voterPublicKey.ToStringValue(), out var tickets))
            {
                var votingRecords = withoutLimitation
                    ? tickets.VotingRecords.Where(vr => vr.IsWithdrawn == false).ToList()
                    : tickets.VotingRecords.Where(vr => vr.UnlockAge >= CurrentAge && vr.IsWithdrawn == false).ToList();

                foreach (var votingRecord in votingRecords)
                {
                    if (votingRecord == null)
                    {
                        continue;
                    }

                    var amount = votingRecord.Count;

                    Api.SendInline(Api.TokenContractAddress, "Transfer", Api.GetFromAddress(), amount);
                    Api.SendInline(Api.DividendsContractAddress, "SubWeights", votingRecord.Weight,
                        _collection.CurrentTermNumberField.GetValue());

                    var blockchainStartTimestamp = _collection.BlockchainStartTimestamp.GetValue();
                    votingRecord.WithdrawTimestamp =
                        blockchainStartTimestamp.ToDateTime().AddMinutes(CurrentAge).ToTimestamp();
                    votingRecord.IsWithdrawn = true;

                    candidatePublicKeys.Add(votingRecord.To);

                    var ticketsCount = _collection.TicketsCountField.GetValue();
                    ticketsCount -= amount;
                    _collection.TicketsCountField.SetValue(ticketsCount);

                    tickets.VotedTickets -= amount;
                }
                
                _collection.TicketsMap.SetValue(voterPublicKey.ToStringValue(), tickets);
            }

            // Handle tickets of all the related candidates.
            foreach (var candidatePublicKey in candidatePublicKeys)
            {
                if (_collection.TicketsMap.TryGet(candidatePublicKey.ToStringValue(), out var ticketsOfCandidate))
                {
                    var votingRecords = withoutLimitation
                        ? tickets.VotingRecords.Where(vr => !vr.IsWithdrawn && vr.From == voterPublicKey).ToList()
                        : tickets.VotingRecords.Where(vr =>
                            vr.UnlockAge >= CurrentAge && !vr.IsWithdrawn && vr.From == voterPublicKey).ToList();

                    foreach (var votingRecord in votingRecords)
                    {
                        if (votingRecord == null)
                            continue;

                        var blockchainStartTimestamp = _collection.BlockchainStartTimestamp.GetValue();
                        votingRecord.WithdrawTimestamp =
                            blockchainStartTimestamp.ToDateTime().AddDays(CurrentAge).ToTimestamp();
                        votingRecord.IsWithdrawn = true;

                        ticketsOfCandidate.ObtainedTickets -= votingRecord.Count;
                    }
                    
                    _collection.TicketsMap.SetValue(candidatePublicKey.ToStringValue(), ticketsOfCandidate);
                }
            }

            return new ActionResult {Success = true};
        }
    }
}