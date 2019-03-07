using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        private Hash Propose(string proposalName, int waitingPeriod, Address fromAddress,
            Address targetAddress, string invokingMethod, params object[] args)
        {
            // packed txn
            byte[] txnData = new Transaction
            {
                From = fromAddress,
                To = targetAddress,
                MethodName = invokingMethod,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args)),
                Type = TransactionType.MsigTransaction,
                Time = Timestamp.FromDateTime(Context.CurrentBlockTime)
            }.ToByteArray();
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            //TimeSpan diff = Context.CurrentBlockTime.AddSeconds(waitingPeriod).ToUniversalTime() - origin;
            var expiredTime = Context.CurrentBlockTime.AddSeconds(waitingPeriod).ToUniversalTime();
            Proposal proposal = new Proposal
            {
                MultiSigAccount = fromAddress,
                Name = proposalName,
                TxnData = ByteString.CopyFrom(txnData),
                ExpiredTime = Timestamp.FromDateTime(expiredTime),
                Status = ProposalStatus.ToBeDecided,
                Proposer = Context.Sender
            };
            State.AuthorizationContract.Propose(proposal);
            return proposal.GetHash();
        }

        private bool IsMiner()
        {
            var roundNumber = State.ConsensusContract.GetCurrentRoundNumber();
            var round = State.ConsensusContract.GetRoundInfo(roundNumber);
            var miners = new Miners {PublicKeys = {round.RealTimeMinersInformation.Keys}};
            return miners.PublicKeys.Any(p => ByteArrayHelpers.FromHexString(p).BytesEqual(Context.RecoverPublicKey()));
        }
        private void CheckAuthority(Address fromAddress = null)
        {
            Assert(fromAddress == null || fromAddress.Equals(Context.Sender), "Not authorized transaction.");
            if (Context.Transaction.Sigs.Count == 1)
                // No need to verify signature again if it is not multi sig account.
                return;
            var auth = State.AuthorizationContract.GetAuthorization(Context.Sender);

            // Get tx hash
            var hash = Context.TransactionId.DumpByteArray();

            // Get pub keys
            var publicKeys = new List<byte[]>();
            foreach (var sig in Context.Transaction.Sigs)
            {
                var publicKey = Context.RecoverPublicKey(sig.ToByteArray(), hash);
                Assert (publicKey != null, "Invalid signature."); // this should never happen.
                publicKeys.Add(publicKey);
            }
            
            // review correctness
            uint provided = publicKeys
                .Select(pubKey => auth.Reviewers.FirstOrDefault(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)))
                .Where(r => !(r is default(Reviewer))).Aggregate<Reviewer, uint>(0, (current, r) => current + r.Weight);
            Assert(provided >= auth.ExecutionThreshold, "Authorization failed without enough approval.");
        }
    }
}