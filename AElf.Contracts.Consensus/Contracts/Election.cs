using System;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
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

        public void AnnounceElection(string alias = "")
        {
            var publicKey = Api.RecoverPublicKey().ToHex();
            // A voter cannot join the election before all his voting record expired.
            if (_collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
            {
                Api.Assert(tickets.VotingRecords.All(t => t.IsExpired(_collection.AgeField.GetValue())), GlobalConfig.VoterCannotAnnounceElection);
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

            if (_collection.AliasesLookupMap.TryGet(alias.ToStringValue(), out var publicKeyOfThisAlias) && publicKey == publicKeyOfThisAlias.Value)
            {
                return;
            }
            
            _collection.AliasesLookupMap.SetValue(alias.ToStringValue(), publicKey.ToStringValue());
            _collection.AliasesMap.SetValue(publicKey.ToStringValue(), alias.ToStringValue());

            // Add this alias to history information of this candidate.
            if (_collection.HistoryMap.TryGet(publicKey.ToStringValue(), out var candidateHistoryInformation))
            {
                if (!candidateHistoryInformation.Aliases.Contains(alias))
                {
                    candidateHistoryInformation.Aliases.Add(alias);
                }

                _collection.HistoryMap.SetValue(publicKey.ToStringValue(), candidateHistoryInformation);
            }
        }

        public void QuitElection()
        {
            Api.UnlockToken(Api.GetFromAddress(), GlobalConfig.LockTokenForElection);
            var candidates = _collection.CandidatesField.GetValue();
            candidates.PublicKeys.Remove(Api.RecoverPublicKey().ToHex());
            _collection.CandidatesField.SetValue(candidates);
        }

        public void Vote(string candidatePublicKey, ulong amount, int lockTime)
        {
            //TODO: Recover after testing.
//            if (lockTime.InRange(1, 3))
//            {
//                lockTime *= 360;
//            }
//            else if (lockTime.InRange(12, 36))
//            {
//                lockTime *= 30;
//            }
//
//            Api.Assert(lockTime.InRange(90, 1080), GlobalConfig.LockDayIllegal);

            Api.Assert(_collection.CandidatesField.GetValue().PublicKeys.Contains(candidatePublicKey),
                GlobalConfig.TargetNotAnnounceElection);

            Api.Assert(!_collection.CandidatesField.GetValue().PublicKeys.Contains(Api.RecoverPublicKey().ToHex()),
                GlobalConfig.CandidateCannotVote);

            Api.LockToken(amount);

            var votingRecord = new VotingRecord
            {
                Count = amount,
                From = Api.RecoverPublicKey().ToHex(),
                To = candidatePublicKey,
                RoundNumber = _collection.CurrentRoundNumberField.GetValue(),
                TransactionId = Api.GetTxnHash(),
                VoteAge = CurrentAge,
                UnlockAge = CurrentAge + (ulong) lockTime,
                TermNumber = _collection.CurrentTermNumberField.GetValue()
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

            tickets.TotalTickets += votingRecord.Count;
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

            candidateTickets.TotalTickets += votingRecord.Count;
            _collection.TicketsMap.SetValue(candidatePublicKey.ToStringValue(), candidateTickets);

            var currentCount = _collection.VotesCountField.GetValue();
            currentCount += 1;
            _collection.VotesCountField.SetValue(currentCount);
            
            var ticketsCount = _collection.TicketsCountField.GetValue();
            ticketsCount += votingRecord.Count;
            _collection.TicketsCountField.SetValue(ticketsCount);

            Api.SendInline(Api.DividendsContractAddress, "AddWeights", votingRecord.Weight,
                _collection.CurrentTermNumberField.GetValue());

            Console.WriteLine($"Voted {amount} tickets to {candidatePublicKey}.");
        }

        public void ReceiveDividends(string candidatePublicKey, ulong amount, int lockDays)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecord =
                    tickets.VotingRecords.FirstOrDefault(vr =>
                        vr.To == candidatePublicKey && vr.Count == amount && vr.LockDaysList.Last() == lockDays);

                if (votingRecord != null)
                {
                    var maxTermNumber = votingRecord.TermNumber +
                                        votingRecord.GetDurationDays(CurrentAge) /
                                        GlobalConfig.DaysEachTerm;
                    Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord, maxTermNumber);
                }
            }
        }

        public void ReceiveDividends(Hash transactionId)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecord = tickets.VotingRecords.FirstOrDefault(vr => vr.TransactionId == transactionId);

                if (votingRecord != null)
                {
                    var maxTermNumber = votingRecord.TermNumber +
                                        votingRecord.GetDurationDays(CurrentAge) /
                                        GlobalConfig.DaysEachTerm;
                    Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord, maxTermNumber);
                }
            }
        }

        public void ReceiveDividends()
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                foreach (var votingRecord in tickets.VotingRecords)
                {
                    var maxTermNumber = votingRecord.TermNumber +
                                        votingRecord.GetDurationDays(CurrentAge) /
                                        GlobalConfig.DaysEachTerm;
                    Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord, maxTermNumber);
                }
            }
        }

        public void Withdraw(string candidatePublicKey, ulong amount, int lockDays)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecord =
                    tickets.VotingRecords.FirstOrDefault(vr =>
                        vr.To == candidatePublicKey && vr.Count == amount && vr.LockDaysList.Last() == lockDays);

                if (votingRecord != null && votingRecord.UnlockAge >= CurrentAge)
                {
                    Api.SendInline(Api.TokenContractAddress, "Transfer", Api.GetFromAddress(), votingRecord.Count);
                    Api.SendInline(Api.DividendsContractAddress, "SubWeights", votingRecord.Weight,
                        _collection.CurrentTermNumberField.GetValue());
                    
                    var ticketsCount = _collection.TicketsCountField.GetValue();
                    ticketsCount -= votingRecord.Count;
                    _collection.TicketsCountField.SetValue(ticketsCount);
                }
            }
        }

        public void Withdraw(Hash transactionId)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecord = tickets.VotingRecords.FirstOrDefault(vr => vr.TransactionId == transactionId);

                if (votingRecord != null && votingRecord.UnlockAge >= CurrentAge)
                {
                    Api.SendInline(Api.TokenContractAddress, "Transfer", Api.GetFromAddress(), votingRecord.Count);
                    Api.SendInline(Api.DividendsContractAddress, "SubWeights", votingRecord.Weight,
                        _collection.CurrentTermNumberField.GetValue());
                    
                    var ticketsCount = _collection.TicketsCountField.GetValue();
                    ticketsCount -= votingRecord.Count;
                    _collection.TicketsCountField.SetValue(ticketsCount);
                }
            }
        }

        public void Withdraw(bool withoutLimitation = false)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecords = withoutLimitation
                    ? tickets.VotingRecords.ToList()
                    : tickets.VotingRecords.Where(vr => vr.UnlockAge >= CurrentAge).ToList();

                foreach (var votingRecord in votingRecords)
                {
                    Api.SendInline(Api.TokenContractAddress, "Transfer", Api.GetFromAddress(), votingRecord.Count);
                    Api.SendInline(Api.DividendsContractAddress, "SubWeights", votingRecord.Weight,
                        _collection.CurrentTermNumberField.GetValue());
                    
                    var ticketsCount = _collection.TicketsCountField.GetValue();
                    ticketsCount -= votingRecord.Count;
                    _collection.TicketsCountField.SetValue(ticketsCount);
                }
            }
        }
    }
}