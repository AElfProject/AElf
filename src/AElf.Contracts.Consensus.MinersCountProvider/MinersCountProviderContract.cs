using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Vote;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

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

        public override Empty ConfigMinersCountProviderContract(ConfigMinersCountProviderContractInput input)
        {
            Assert(!State.Configured.Value, "Initial miners count already set.");
            State.Mode.Value = input.Mode;
            State.BlockchainStartTimestamp.Value = input.BlockchainStartTimestamp;
            if (input.Mode == 0)
            {
                // Create Miners Count Voting Item.
                var votingRegisterInput = new VotingRegisterInput
                {
                    IsLockToken = false,
                    AcceptedCurrency = Context.Variables.NativeSymbol,
                    TotalSnapshotNumber = long.MaxValue,
                    StartTimestamp = DateTime.MinValue.ToUniversalTime().ToTimestamp(),
                    EndTimestamp = DateTime.MaxValue.ToUniversalTime().ToTimestamp(),
                    Options = {Enumerable.Range(1, 50).Select(i => i.ToString())}
                };
                State.VoteContract.Register.Send(votingRegisterInput);

                // In order to calculate voting item id.
                votingRegisterInput.Options.Clear();
                State.MinersCountVotingItemId.Value = Hash.FromTwoHashes(Hash.FromMessage(votingRegisterInput),
                    Hash.FromMessage(Context.Self));

                Context.LogDebug(() => $"Miners Count Voting Item Id: {State.MinersCountVotingItemId.Value.ToHex()}");
            }

            State.MinersCount.Value = input.MinersCountInitialValue;
            State.Configured.Value = true;
            State.Step.Value = input.IncreaseStep;
            return new Empty();
        }

        public override SInt32Value GetMinersCount(Empty input)
        {
            Assert(State.Configured.Value, "Miners Count Provider Contract not configured.");
            if (State.Mode.Value == 0)
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

            var offset = 0;
            if (State.Mode.Value == 1)
            {
                offset = (int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value.ToDateTime())
                    .TotalMinutes;
            }

            if (State.Mode.Value == 2)
            {
                offset = ((int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value.ToDateTime())
                    .TotalDays).Div(12);
            }

            var minersCount = State.MinersCount.Value.Add(offset.Mul(State.Step.Value));
            Context.LogDebug(() => $"Current miners count: {minersCount}");
            return new SInt32Value {Value = minersCount};
        }
    }
}