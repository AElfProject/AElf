using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus.Contracts
{
    public class Election
    {
        private readonly DataCollection _collection;

        public Election(DataCollection collection)
        {
            _collection = collection;
        }

        public void AnnounceElection()
        {
            // A voter cannot join the election before all his voting record expired.
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                Api.Assert(tickets.VotingRecords.All(t => t.IsExpired()), GlobalConfig.VoterCannotAnnounceElection);
            }
            
            Api.LockToken(GlobalConfig.LockTokenForElection);
            var candidates = _collection.CandidatesField.GetValue();
            candidates.PublicKeys.Add(Api.RecoverPublicKey().ToHex());
            _collection.CandidatesField.SetValue(candidates);
        }

        public void QuitElection()
        {
            Api.WithdrawToken(Api.GetFromAddress(), GlobalConfig.LockTokenForElection);
            var candidates = _collection.CandidatesField.GetValue();
            candidates.PublicKeys.Remove(Api.RecoverPublicKey().ToHex());
            _collection.CandidatesField.SetValue(candidates);
        }

        public void Vote(string candidatePublicKey, ulong amount, int lockAmount)
        {
            if (lockAmount.InRange(1, 3))
            {
                lockAmount *= 360;
            }
            else if (lockAmount.InRange(12, 36))
            {
                lockAmount *= 30;
            }

            Api.Assert(lockAmount.InRange(90, 1080), GlobalConfig.LockDayIllegal);

            Api.Assert(_collection.CandidatesField.GetValue().PublicKeys.Contains(candidatePublicKey),
                GlobalConfig.TargetNotAnnounceElection);

            Api.Assert(!_collection.CandidatesField.GetValue().PublicKeys.Contains(Api.RecoverPublicKey().ToHex()),
                GlobalConfig.CandidateCannotVote);

            Api.LockToken(amount);

            var ageOfBlockchain = _collection.AgeField.GetValue();

            var votingRecord = new VotingRecord
            {
                Count = amount,
                From = Api.RecoverPublicKey().ToHex(),
                To = candidatePublicKey,
                RoundNumber = _collection.CurrentRoundNumberField.GetValue(),
                TransactionId = Api.GetTxnHash(),
                VoteTimestamp = DateTime.UtcNow.ToTimestamp(),
                UnlockAge = ageOfBlockchain + (ulong) lockAmount,
                TermNumber = _collection.CurrentTermNumberField.GetValue()
            };
            votingRecord.LockDaysList.Add((uint) lockAmount);

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

            Api.SendInline(Api.DividendsContractAddress, "AddWeights", votingRecord.Weight,
                _collection.CurrentTermNumberField.GetValue());

            Console.WriteLine($"Voted {amount} tickets to {candidatePublicKey}.");
        }

        public void GetDividends(string candidatePublicKey, ulong amount, int lockDays)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecord =
                    tickets.VotingRecords.FirstOrDefault(vr =>
                        vr.To == candidatePublicKey && vr.Count == amount && vr.LockDaysList.Last() == lockDays);

                if (votingRecord != null)
                {
                    var maxTermNumber = votingRecord.TermNumber + votingRecord.DurationDays / GlobalConfig.DaysEachTerm;
                    Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord, maxTermNumber);
                }
            }
        }

        public void GetDividends(Hash transactionId)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecord = tickets.VotingRecords.FirstOrDefault(vr => vr.TransactionId == transactionId);

                if (votingRecord != null)
                {
                    var maxTermNumber = votingRecord.TermNumber + votingRecord.DurationDays / GlobalConfig.DaysEachTerm;
                    Api.SendInline(Api.DividendsContractAddress, "TransferDividends", votingRecord, maxTermNumber);
                }
            }
        }

        public void GetDividends()
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                foreach (var votingRecord in tickets.VotingRecords)
                {
                    var maxTermNumber = votingRecord.TermNumber + votingRecord.DurationDays / GlobalConfig.DaysEachTerm;
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

                if (votingRecord != null && votingRecord.UnlockAge >= _collection.AgeField.GetValue())
                {
                    Api.Call(Api.TokenContractAddress, "Transfer",
                        ParamsPacker.Pack(new List<object> {Api.GetFromAddress(), votingRecord.Count}));
                    Api.Call(Api.DividendsContractAddress, "SubWeights",
                        ParamsPacker.Pack(new List<object>
                            {votingRecord.Weight, _collection.CurrentTermNumberField.GetValue()}));
                }
            }
        }

        public void Withdraw(Hash transactionId)
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecord = tickets.VotingRecords.FirstOrDefault(vr => vr.TransactionId == transactionId);

                if (votingRecord != null && votingRecord.UnlockAge >= _collection.AgeField.GetValue())
                {
                    Api.Call(Api.TokenContractAddress, "Transfer",
                        ParamsPacker.Pack(new List<object> {Api.GetFromAddress(), votingRecord.Count}));
                    Api.Call(Api.DividendsContractAddress, "SubWeights",
                        ParamsPacker.Pack(new List<object>
                            {votingRecord.Weight, _collection.CurrentTermNumberField.GetValue()}));
                }
            }
        }

        public void Withdraw()
        {
            if (_collection.TicketsMap.TryGet(Api.RecoverPublicKey().ToHex().ToStringValue(), out var tickets))
            {
                var votingRecords = tickets.VotingRecords.Where(vr => vr.UnlockAge >= _collection.AgeField.GetValue());

                foreach (var votingRecord in votingRecords)
                {
                    Api.Call(Api.TokenContractAddress, "Transfer",
                        ParamsPacker.Pack(new List<object> {Api.GetFromAddress(), votingRecord.Count}));
                    Api.Call(Api.DividendsContractAddress, "SubWeights",
                        ParamsPacker.Pack(new List<object>
                            {votingRecord.Weight, _collection.CurrentTermNumberField.GetValue()}));
                }
            }
        }
    }
}