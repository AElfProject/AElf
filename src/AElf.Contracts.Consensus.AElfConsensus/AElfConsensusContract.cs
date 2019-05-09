using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.MinersCountProvider;
using AElf.Contracts.Election;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContract : AElfConsensusContractContainer.AElfConsensusContractBase
    {
        public override Empty InitialAElfConsensusContract(InitialAElfConsensusContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.TimeEachTerm.Value = input.IsSideChain || input.IsTermStayOne
                ? int.MaxValue
                : int.Parse(Context.Variables.TimeEachTerm);

            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();

            State.MinersCountProviderContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(input.MinersCountProviderContractSystemName);

            if (input.IsTermStayOne || input.IsSideChain)
            {
                return new Empty();
            }

            State.BaseTimeUnit.Value = int.Parse(Context.Variables.BaseTimeUnit);

            State.ElectionContractSystemName.Value = input.ElectionContractSystemName;

            State.ElectionContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(input.ElectionContractSystemName);

            State.ElectionContract.RegisterElectionVotingEvent.Send(new Empty());

            State.ElectionContract.CreateTreasury.Send(new Empty());

            State.ElectionContract.RegisterToTreasury.Send(new Empty());

            State.MinersCountProviderContract.InitialMinersCountProviderContract.Send(
                new InitialMinersCountProviderContractInput
                {
                    // To Lock and Unlock tokens of voters.
                    TokenContractSystemName = input.TokenContractSystemName,
                    VoteContractSystemName = input.VoteContractSystemName,
                });

            return new Empty();
        }

        public override Empty FirstRound(Round input)
        {
            Assert(input.RoundNumber == 1, "Invalid round number.");

            Assert(input.RealTimeMinersInformation.Any(), "No miner in input data.");

            State.CurrentTermNumber.Value = 1;
            State.CurrentRoundNumber.Value = 1;
            State.FirstRoundNumberOfEachTerm[1] = 1L;
            SetBlockchainStartTimestamp(input.GetStartTime().ToTimestamp());
            State.MiningInterval.Value = input.GetMiningInterval();

            if (State.ElectionContract.Value != null)
            {
                State.ElectionContract.SetInitialMiners.Send(new PublicKeysList
                {
                    Value =
                    {
                        input.RealTimeMinersInformation.Keys.Select(k =>
                            ByteString.CopyFrom(ByteArrayHelpers.FromHexString(k)))
                    }
                });
            }

            var miners = new Miners {TermNumber = 1};
            miners.PublicKeys.AddRange(input.RealTimeMinersInformation.Keys.Select(k =>
                ByteString.CopyFrom(ByteArrayHelpers.FromHexString(k))));
            miners.TermNumber = 1;
            SetMiners(miners);

            Assert(TryToAddRoundInformation(input), "Failed to add round information.");
            return new Empty();
        }

        #region UpdateValue

        public override Empty UpdateValue(UpdateValueInput input)
        {
            Assert(TryToGetCurrentRoundInformation(out var round), "Round information not found.");
            Assert(input.RoundId == round.RoundId, "Round Id not matched.");

            var publicKey = Context.RecoverPublicKey().ToHex();

            round.RealTimeMinersInformation[publicKey].ActualMiningTime = input.ActualMiningTime;
            round.RealTimeMinersInformation[publicKey].ProducedBlocks = input.ProducedBlocks;
            var producedTinyBlocks = round.RealTimeMinersInformation[publicKey].ProducedTinyBlocks;
            round.RealTimeMinersInformation[publicKey].ProducedTinyBlocks = producedTinyBlocks.Add(1);

            round.RealTimeMinersInformation[publicKey].Signature = input.Signature;
            round.RealTimeMinersInformation[publicKey].OutValue = input.OutValue;
            round.RealTimeMinersInformation[publicKey].PromisedTinyBlocks = input.PromiseTinyBlocks;
            round.RealTimeMinersInformation[publicKey].SupposedOrderOfNextRound = input.SupposedOrderOfNextRound;
            round.RealTimeMinersInformation[publicKey].FinalOrderOfNextRound = input.SupposedOrderOfNextRound;

            round.RealTimeMinersInformation[publicKey].EncryptedInValues.Add(input.EncryptedInValues);
            foreach (var decryptedPreviousInValue in input.DecryptedPreviousInValues)
            {
                round.RealTimeMinersInformation[decryptedPreviousInValue.Key].DecryptedPreviousInValues
                    .Add(publicKey, decryptedPreviousInValue.Value);
            }

            foreach (var previousInValue in input.MinersPreviousInValues)
            {
                if (previousInValue.Key == publicKey)
                {
                    continue;
                }

                var filledValue = round.RealTimeMinersInformation[previousInValue.Key].PreviousInValue;
                if (filledValue != null && filledValue != previousInValue.Value)
                {
                    Context.LogDebug(() => $"Something wrong happened to previous in value of {previousInValue.Key}.");
                    State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
                    {
                        PublicKey = publicKey,
                        IsEvilNode = true
                    });
                }

                round.RealTimeMinersInformation[previousInValue.Key].PreviousInValue = previousInValue.Value;
            }

            foreach (var tuneOrder in input.TuneOrderInformation)
            {
                round.RealTimeMinersInformation[tuneOrder.Key].FinalOrderOfNextRound = tuneOrder.Value;
            }

            // For first round of each term, no one need to publish in value.
            if (input.PreviousInValue != Hash.Empty)
            {
                round.RealTimeMinersInformation[publicKey].PreviousInValue = input.PreviousInValue;
            }

            Assert(TryToUpdateRoundInformation(round), "Failed to update round information.");

            TryToFindLastIrreversibleBlock();

            return new Empty();
        }

        #endregion

        public override Empty UpdateTinyBlockInformation(TinyBlockInput input)
        {
            Assert(TryToGetCurrentRoundInformation(out var round), "Round information not found.");
            Assert(input.RoundId == round.RoundId, "Round Id not matched.");

            var publicKey = Context.RecoverPublicKey().ToHex();

            round.RealTimeMinersInformation[publicKey].ActualMiningTime = input.ActualMiningTime;
            round.RealTimeMinersInformation[publicKey].ProducedBlocks = input.ProducedBlocks;
            var producedTinyBlocks = round.RealTimeMinersInformation[publicKey].ProducedTinyBlocks;
            round.RealTimeMinersInformation[publicKey].ProducedTinyBlocks = producedTinyBlocks.Add(1);

            Assert(TryToUpdateRoundInformation(round), "Failed to update round information.");

            return new Empty();
        }

        #region NextRound

        public override Empty NextRound(Round input)
        {
            if (TryToGetRoundNumber(out var currentRoundNumber))
            {
                Assert(currentRoundNumber < input.RoundNumber, "Incorrect round number for next round.");
            }

            if (currentRoundNumber == 1)
            {
                var actualBlockchainStartTimestamp = input.GetStartTime().ToTimestamp();
                SetBlockchainStartTimestamp(actualBlockchainStartTimestamp);
                State.MinersCountProviderContract.ConfigMinersCountProviderContract.Send(new ConfigMinersCountProviderContractInput
                {
                    MinersCountInitialValue = input.RealTimeMinersInformation.Count,
                    Mode = 1,
                    BlockchainStartTimestamp = actualBlockchainStartTimestamp,
                    IncreaseStep = 1
                });
            }

            Assert(TryToGetCurrentRoundInformation(out _), "Failed to get current round information.");
            Assert(TryToAddRoundInformation(input), "Failed to add round information.");
            Assert(TryToUpdateRoundNumber(input.RoundNumber), "Failed to update round number.");
            TryToFindLastIrreversibleBlock();
            return new Empty();
        }

        /// <summary>
        /// Basically this method only used for testing LIB finding logic.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value GetLIBOffset(Empty input)
        {
            return new SInt64Value {Value = CalculateLastIrreversibleBlock(out var offset) ? offset : 0};
        }

        private void TryToFindLastIrreversibleBlock()
        {
            if (CalculateLastIrreversibleBlock(out var offset))
            {
                Context.LogDebug(() => $"LIB found, offset is {offset}");
                Context.Fire(new IrreversibleBlockFound()
                {
                    Offset = offset
                });
            }
        }

        private bool CalculateLastIrreversibleBlock(out long offset)
        {
            offset = 0;

            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var currentRoundMiners = currentRound.RealTimeMinersInformation;

                var minersCount = currentRoundMiners.Count;

                var minimumCount = ((int) ((minersCount * 2d) / 3)) + 1;

                if (minersCount == 1)
                {
                    // Single node will set every previous block as LIB.
                    offset = 1;
                    return true;
                }

                var validMinersOfCurrentRound = currentRoundMiners.Values.Where(m => m.OutValue != null).ToList();
                var validMinersCountOfCurrentRound = validMinersOfCurrentRound.Count;

                if (validMinersCountOfCurrentRound >= minimumCount)
                {
                    offset = minimumCount;
                    return true;
                }

                // Current round is not enough to find LIB.

                var publicKeys = new HashSet<string>(validMinersOfCurrentRound.Select(m => m.PublicKey));

                if (TryToGetPreviousRoundInformation(out var previousRound))
                {
                    var preRoundMiners = previousRound.RealTimeMinersInformation.Values.OrderByDescending(m => m.Order)
                        .Select(m => m.PublicKey).ToList();

                    var traversalBlocksCount = publicKeys.Count;

                    for (var i = 0; i < minersCount; i++)
                    {
                        if (++traversalBlocksCount > minersCount)
                        {
                            return false;
                        }

                        var miner = preRoundMiners[i];

                        if (previousRound.RealTimeMinersInformation[miner].OutValue != null)
                        {
                            if (!publicKeys.Contains(miner))
                                publicKeys.Add(miner);
                        }

                        if (publicKeys.Count >= minimumCount)
                        {
                            offset = minimumCount;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void SetBlockchainStartTimestamp(Timestamp timestamp)
        {
            Context.LogDebug(() => $"Set start timestamp to {timestamp}");
            State.BlockchainStartTimestamp.Value = timestamp;
        }

        private bool TryToUpdateRoundNumber(long roundNumber)
        {
            var oldRoundNumber = State.CurrentRoundNumber.Value;
            if (roundNumber != 1 && oldRoundNumber + 1 != roundNumber)
            {
                return false;
            }

            State.CurrentRoundNumber.Value = roundNumber;
            return true;
        }

        #endregion

        private bool TryToAddRoundInformation(Round round)
        {
            var ri = State.Rounds[round.RoundNumber];
            if (ri != null)
            {
                return false;
            }

            State.Rounds[round.RoundNumber] = round;
            return true;
        }

        private bool TryToUpdateRoundInformation(Round round)
        {
            var ri = State.Rounds[round.RoundNumber];
            if (ri == null)
            {
                return false;
            }

            State.Rounds[round.RoundNumber] = round;
            return true;
        }
    }
}