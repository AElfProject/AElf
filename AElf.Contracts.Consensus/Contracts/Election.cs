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
            Api.LockToken(GlobalConfig.LockTokenForElection);
            var candidates = _collection.CandidatesField.GetValue();
            candidates.PublicKeys.Add(Api.GetPublicKeyToHex());
            _collection.CandidatesField.SetValue(candidates);
        }

        public void QuitElection()
        {
            Api.Call(Api.TokenContractAddress, "Transfer",
                ParamsPacker.Pack(new List<object> {Api.GetFromAddress(), GlobalConfig.LockTokenForElection}));
            var candidates = _collection.CandidatesField.GetValue();
            candidates.PublicKeys.Remove(Api.GetPublicKeyToHex());
            _collection.CandidatesField.SetValue(candidates);
        }

        public void Vote(string candidatePublicKey, ulong amount, int lockDays)
        {
            if (lockDays.InRange(1, 3))
            {
                lockDays *= 360;
            }
            else if (lockDays.InRange(12, 36))
            {
                lockDays *= 30;
            }

            Api.Assert(lockDays.InRange(90, 1080), "Lock days is illegal.");

            var ageOfBlockchain = _collection.AgeField.GetValue();

            var votingRecord = new VotingRecord
            {
                Count = amount,
                From = Api.GetPublicKeyToHex(),
                To = candidatePublicKey,
                RoundNumber = _collection.CurrentRoundNumberField.GetValue(),
                TransactionId = Api.GetTxnHash(),
                VoteTimestamp = DateTime.UtcNow.ToTimestamp(),
                UnlockAge = ageOfBlockchain + (ulong) lockDays,
                TermNumber = _collection.CurrentTermNumberField.GetValue()
            };

            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
            {
                tickets.VotingRecords.Add(votingRecord);
            }
            else
            {
                tickets = new Tickets();
                tickets.VotingRecords.Add(votingRecord);
            }

            tickets.TotalTickets += votingRecord.Count;
            _collection.TicketsMap.SetValue(Api.GetPublicKeyToHex().ToStringValue(), tickets);

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

            Api.Call(Api.DividendsContractAddress, "AddWeights",
                ParamsPacker.Pack(new List<object>
                    {votingRecord.Weight, _collection.CurrentTermNumberField.GetValue()}));
        }

        public void GetDividends(string candidatePublicKey, ulong amount, int lockDays)
        {
            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
            {
                var votingRecord =
                    tickets.VotingRecords.FirstOrDefault(vr =>
                        vr.To == candidatePublicKey && vr.Count == amount && vr.LockDaysList.Last() == lockDays);

                if (votingRecord != null)
                {
                    var maxTermNumber = votingRecord.TermNumber + votingRecord.DurationDays / GlobalConfig.DaysEachTerm;
                    Api.Call(Api.DividendsContractAddress, "TransferDividends",
                        ParamsPacker.Pack(new List<object> {votingRecord, maxTermNumber}));
                }
            }
        }
        
        public void GetDividends(Hash transactionId)
        {
            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
            {
                var votingRecord = tickets.VotingRecords.FirstOrDefault(vr => vr.TransactionId == transactionId);

                if (votingRecord != null)
                {
                    var maxTermNumber = votingRecord.TermNumber + votingRecord.DurationDays / GlobalConfig.DaysEachTerm;
                    Api.Call(Api.DividendsContractAddress, "TransferDividends",
                        ParamsPacker.Pack(new List<object> {votingRecord, maxTermNumber}));
                }
            }
        }
        
        public void GetDividends()
        {
            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
            {
                foreach (var votingRecord in tickets.VotingRecords)
                {
                    var maxTermNumber = votingRecord.TermNumber + votingRecord.DurationDays / GlobalConfig.DaysEachTerm;
                    Api.Call(Api.DividendsContractAddress, "TransferDividends",
                        ParamsPacker.Pack(new List<object> {votingRecord, maxTermNumber}));
                }
            }
        }

        public void Withdraw(string candidatePublicKey, ulong amount, int lockDays)
        {
            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
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
            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
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
            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
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