using System;
using System.Linq;
using AElf.Consensus.AElfConsensus;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Vote;

namespace AElf.Contracts.Consensus.MinersCountProvider
{
    public partial class MinersCountProviderContract : MinersCountProviderContractContainer.MinersCountProviderContractBase
    {
        public override Empty InitialMinersCountProviderContract(InitialMinersCountProviderContractInput input)
        {
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();

            State.VoteContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(input.VoteContractSystemName);
            State.TokenContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(input.TokenContractSystemName);
            State.Mode.Value = input.Mode;

            if (input.Mode == MinersCountMode.Vote)
            {
                // Create Miners Count Voting Item.
                var votingRegisterInput = new VotingRegisterInput
                {
                    IsLockToken = false,
                    AcceptedCurrency = Context.Variables.NativeSymbol,
                    TotalSnapshotNumber = long.MaxValue,
                    StartTimestamp = DateTime.MinValue.ToUniversalTime().ToTimestamp(),
                    EndTimestamp = DateTime.MaxValue.ToUniversalTime().ToTimestamp(),
                    Options = {Enumerable.Range(1, 100).Select(i => i.ToString())}
                };
                State.VoteContract.Register.Send(votingRegisterInput);

                // In order to calculate voting item id.
                votingRegisterInput.Options.Clear();
                State.MinersCountVotingItemId.Value = Hash.FromTwoHashes(Hash.FromMessage(votingRegisterInput),
                    Hash.FromMessage(Context.Self));

                Context.LogDebug(() => $"Miners Count Voting Item Id: {State.MinersCountVotingItemId.Value.ToHex()}");
            }

            return new Empty();
        }

        public override Empty VoteMinersCount(VoteMinersCountInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
            }
            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = input.Amount,
                LockId = Context.TransactionId,
                Symbol = Context.Variables.NativeSymbol,
                Usage = "Voting for miners count."
            });
            State.VoteContract.Vote.Send(new VoteInput
            {
                VotingItemId = State.MinersCountVotingItemId.Value,
                Option = input.MinersCount.ToString(),
                Amount = input.Amount,
                VoteId = Context.TransactionId,
                Voter = Context.Sender
            });
            return new Empty();
        }

        public override SInt32Value GetMinersCount(Empty input)
        {
            if (State.Mode.Value == MinersCountMode.Vote && State.MinersCountVotingItemId.Value != null)
            {
                // Check voting item of Miners Count.
                var votingResult = State.VoteContract.GetLatestVotingResult.Call(State.MinersCountVotingItemId.Value);
                if (votingResult.Results.Any())
                {
                    var count = votingResult.Results.OrderByDescending(p => p.Value).Select(p => p.Key).First();
                    return new SInt32Value {Value = int.Parse(count)};
                }
            }
            
            return new SInt32Value();
        }
    }
}