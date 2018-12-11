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

            Api.Assert(lockDays.InRange(90,1080), "Lock days is illegal.");

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
            };

            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
            {
                tickets.VotingRecords.Add(votingRecord);
            }
            else
            {
                tickets.ExpiredTickets = 0;
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
                candidateTickets.VotingRecords.Add(votingRecord);
            }
            candidateTickets.TotalTickets += votingRecord.Count;
            _collection.TicketsMap.SetValue(candidatePublicKey.ToStringValue(), candidateTickets);

            Api.Call(Api.DividendsContractAddress, "AddWeights",
                ParamsPacker.Pack(new List<object> {votingRecord.Weight}));
        }

        public void Withdraw(string candidatePublicKey, ulong amount, int lockDays)
        {
            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
            {
                var votingRecord =
                    tickets.VotingRecords.FirstOrDefault(vr =>
                        vr.To == candidatePublicKey && vr.Count == amount && vr.LockDaysList.Last() == lockDays);

                var ageOfBlockchain = _collection.AgeField.GetValue();
                
                if (votingRecord != null && votingRecord.UnlockAge >= ageOfBlockchain)
                {
                    Api.Call(Api.TokenContractAddress, "Transfer",
                        ParamsPacker.Pack(new List<object> {Api.GetFromAddress(), amount}));
                    Api.Call(Api.DividendsContractAddress, "SubWeights",
                        ParamsPacker.Pack(new List<object> {votingRecord.Weight}));
                }
            }
        }
    }
}