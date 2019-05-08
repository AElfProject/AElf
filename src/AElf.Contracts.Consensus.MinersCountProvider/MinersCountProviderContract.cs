using System;
using System.Linq;
using AElf.Consensus.AElfConsensus;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Vote;

namespace AElf.Contracts.Consensus.MinersCountProvider
{
    public class MinersCountProviderContract : MinersCountProviderContractContainer.MinersCountProviderContractBase
    {
        public override Empty InitialMinersCountProviderContract(InitialMinersCountProviderContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
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

            State.Initialized.Value = true;

            return new Empty();
        }

        /// <summary>
        /// Only for testing.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty VoteMinersCount(VoteMinersCountInput input)
        {
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

        public override Empty SetInitialMinersCount(SInt32Value input)
        {
            Assert(!State.IsInitialMinersCountSet.Value, "Initial miners count already set.");
            State.MinersCount.Value = input.Value;
            State.IsInitialMinersCountSet.Value = true;
            return new Empty();
        }

        public override SInt32Value GetMinersCount(Empty input)
        {
            if (State.Mode.Value == MinersCountMode.Vote)
            {
                Assert(State.MinersCountVotingItemId.Value != null, "Corresponding voting item id shouldn't be null.");
                // Check voting item of Miners Count.
                var votingResult = State.VoteContract.GetLatestVotingResult.Call(State.MinersCountVotingItemId.Value);
                if (votingResult.Results.Any())
                {
                    var count = votingResult.Results.OrderByDescending(p => p.Value).Select(p => p.Key).First();
                    return new SInt32Value {Value = int.Parse(count)};
                }
            }

            if (State.Mode.Value == MinersCountMode.IncreaseEveryHour)
            {
                
            }

            return new SInt32Value();
        }
    }
}