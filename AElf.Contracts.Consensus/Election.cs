using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus
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

        public void Vote(string candidatePublicKey, ulong amount)
        {
            var votingRecord = new VotingRecord
            {
                Count = amount,
                From = Api.GetPublicKeyToHex(),
                To = candidatePublicKey,
                RoundNumber = _collection.CurrentRoundNumberField.GetValue(),
                TransactionId = Api.GetTxnHash(),
                VoteTimestamp = DateTime.UtcNow.ToTimestamp()
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
        }

        public void Withdraw(string candidatePublicKey, ulong amount)
        {
            if (_collection.TicketsMap.TryGet(Api.GetPublicKeyToHex().ToStringValue(), out var tickets))
            {
                var record =
                    tickets.VotingRecords.FirstOrDefault(vr => vr.To == candidatePublicKey && vr.Count == amount);
                if (record != null)
                {
                    Api.Call(Api.TokenContractAddress, "Transfer",
                        ParamsPacker.Pack(new List<object> {Api.GetFromAddress(), amount}));
                }
            }
        }
    }
}