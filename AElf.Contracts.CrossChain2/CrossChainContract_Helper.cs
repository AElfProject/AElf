using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain2
{
    public partial class CrossChainContract
    {
        private Hash Propose(string proposalName, double waitingPeriod, Address fromAddress,
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
                Time = Timestamp.FromDateTime(CurrentBlockTime)
            }.ToByteArray();
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = CurrentBlockTime.AddSeconds(waitingPeriod).ToUniversalTime() - origin;

            Proposal proposal = new Proposal
            {
                MultiSigAccount = fromAddress,
                Name = proposalName,
                TxnData = ByteString.CopyFrom(txnData),
                ExpiredTime = diff.TotalSeconds,
                Status = ProposalStatus.ToBeDecided,
                Proposer = Context.Sender
            };
            State.AuthorizationContract.Propose(proposal);
            return proposal.GetHash();
        }
    }
}