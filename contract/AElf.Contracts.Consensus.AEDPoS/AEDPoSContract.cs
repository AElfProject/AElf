using System.Linq;
using AElf.Contracts.Election;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract : AEDPoSContractImplContainer.AEDPoSContractImplBase
    {
        #region Initial

        public override Empty InitialAElfConsensusContract(InitialAElfConsensusContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.TimeEachTerm.Value = input.IsSideChain || input.IsTermStayOne
                ? int.MaxValue
                : input.TimeEachTerm;
            
            State.MinerIncreaseInterval.Value = input.MinerIncreaseInterval;

            Context.LogDebug(() => $"Time each term: {State.TimeEachTerm.Value} seconds.");

            if (input.IsTermStayOne || input.IsSideChain)
            {
                State.IsMainChain.Value = false;
                return new Empty();
            }

            State.IsMainChain.Value = true;

            State.ElectionContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);

            State.ElectionContract.RegisterElectionVotingEvent.Send(new Empty());

            State.ElectionContract.CreateTreasury.Send(new Empty());

            State.ElectionContract.RegisterToTreasury.Send(new Empty());

            return new Empty();
        }

        #endregion

        #region FirstRound

        public override Empty FirstRound(Round input)
        {
            Assert(State.CurrentRoundNumber.Value == 0, "Not first round.");
            Assert(input.RoundNumber == 1, "Invalid round number.");
            Assert(input.RealTimeMinersInformation.Any(), "No miner in input data.");

            State.CurrentTermNumber.Value = 1;
            State.CurrentRoundNumber.Value = 1;
            State.FirstRoundNumberOfEachTerm[1] = 1L;
            SetBlockchainStartTimestamp(input.GetStartTime());
            State.MiningInterval.Value = input.GetMiningInterval();
            State.MainChainCurrentMinerList.Value = new MinerList
            {
                Pubkeys = {input.RealTimeMinersInformation.Keys.Select(k => k.ToByteString())}
            };

            if (State.ElectionContract.Value != null)
            {
                State.ElectionContract.ConfigElectionContract.Send(new ConfigElectionContractInput
                {
                    MinerList = {input.RealTimeMinersInformation.Keys},
                    TimeEachTerm = State.TimeEachTerm.Value,
                    MinerIncreaseInterval = State.MinerIncreaseInterval.Value
                });
            }

            var minerList = new MinerList
                {Pubkeys = {input.RealTimeMinersInformation.Keys.Select(k => k.ToByteString())}};
            SetMinerListOfCurrentTerm(minerList);

            Assert(TryToAddRoundInformation(input), "Failed to add round information.");
            return new Empty();
        }

        #endregion

        #region UpdateValue

        public override Empty UpdateValue(UpdateValueInput input)
        {
            Assert(TryToGetCurrentRoundInformation(out var round), "Round information not found.");
            Assert(input.RoundId == round.RoundId, "Round Id not matched.");

            var publicKey = Context.RecoverPublicKey().ToHex();

            var minerInRound = round.RealTimeMinersInformation[publicKey];
            minerInRound.ActualMiningTimes.Add(input.ActualMiningTime);
            minerInRound.ProducedBlocks = input.ProducedBlocks;
            minerInRound.ProducedTinyBlocks = round.RealTimeMinersInformation[publicKey].ProducedTinyBlocks.Add(1);
            minerInRound.Signature = input.Signature;
            minerInRound.OutValue = input.OutValue;
            minerInRound.SupposedOrderOfNextRound = input.SupposedOrderOfNextRound;
            minerInRound.FinalOrderOfNextRound = input.SupposedOrderOfNextRound;
            minerInRound.ImpliedIrreversibleBlockHeight = input.ImpliedIrreversibleBlockHeight;

            PerformSecretSharing(input, minerInRound, round, publicKey);

            UpdatePreviousInValues(input, publicKey, round);

            foreach (var tuneOrder in input.TuneOrderInformation)
            {
                round.RealTimeMinersInformation[tuneOrder.Key].FinalOrderOfNextRound = tuneOrder.Value;
            }

            // For first round of each term, no one need to publish in value.
            if (input.PreviousInValue != Hash.Empty)
            {
                minerInRound.PreviousInValue = input.PreviousInValue;
            }
            
            if (!TryToUpdateRoundInformation(round))
            {
                Assert(false, "Failed to update round information.");
            }

            var irreversibleBlockHeight = CalculateLastIrreversibleBlockHeight();
            if (irreversibleBlockHeight != 0)
            {
                Context.Fire(new IrreversibleBlockFound
                {
                    IrreversibleBlockHeight = irreversibleBlockHeight
                });
            }
            
            return new Empty();
        }

        private static void PerformSecretSharing(UpdateValueInput input, MinerInRound minerInRound, Round round,
            string publicKey)
        {
            minerInRound.EncryptedInValues.Add(input.EncryptedInValues);
            foreach (var decryptedPreviousInValue in input.DecryptedPreviousInValues)
            {
                round.RealTimeMinersInformation[decryptedPreviousInValue.Key].DecryptedPreviousInValues
                    .Add(publicKey, decryptedPreviousInValue.Value);
            }
        }

        private void UpdatePreviousInValues(UpdateValueInput input, string publicKey, Round round)
        {
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
        }

        #endregion

        #region UpdateTinyBlockInformation

        public override Empty UpdateTinyBlockInformation(TinyBlockInput input)
        {
            Assert(TryToGetCurrentRoundInformation(out var round), "Round information not found.");
            if (input.RoundId != round.RoundId)
            {
                Context.LogDebug(() => "Round Id not matched.");
            }
            Assert(input.RoundId == round.RoundId, "Round Id not matched.");

            var publicKey = Context.RecoverPublicKey().ToHex();
            
            round.RealTimeMinersInformation[publicKey].ActualMiningTimes.Add(input.ActualMiningTime);
            round.RealTimeMinersInformation[publicKey].ProducedBlocks = input.ProducedBlocks;
            var producedTinyBlocks = round.RealTimeMinersInformation[publicKey].ProducedTinyBlocks;
            round.RealTimeMinersInformation[publicKey].ProducedTinyBlocks = producedTinyBlocks.Add(1);

            Assert(TryToUpdateRoundInformation(round), "Failed to update round information.");

            return new Empty();
        }

        #endregion

        #region NextRound

        public override Empty NextRound(Round input)
        {
            if (TryToGetRoundNumber(out var currentRoundNumber))
            {
                Assert(currentRoundNumber < input.RoundNumber, "Incorrect round number for next round.");
            }

            if (currentRoundNumber == 1)
            {
                var actualBlockchainStartTimestamp = input.GetStartTime();
                SetBlockchainStartTimestamp(actualBlockchainStartTimestamp);
                if (State.IsMainChain.Value)
                {
                    var minersCount = GetMinersCount(input);
                    if (minersCount != 0 && State.ElectionContract.Value != null)
                    {
                        State.ElectionContract.UpdateMinersCount.Send(new UpdateMinersCountInput
                        {
                            MinersCount = minersCount
                        });
                    }
                }
            }

            Assert(TryToGetCurrentRoundInformation(out _), "Failed to get current round information.");
            Assert(TryToAddRoundInformation(input), "Failed to add round information.");
            Assert(TryToUpdateRoundNumber(input.RoundNumber), "Failed to update round number.");

            return new Empty();
        }

        #endregion

        #region UpdateConsensusInformation

        public override Empty UpdateConsensusInformation(ConsensusInformation input)
        {
            Assert(!State.IsMainChain.Value, "Only side chain can update consensus information.");
            // For now we just extract the miner list from main chain consensus information, then update miners list.
            if (input == null || input.Value.IsEmpty)
                return new Empty();
            var consensusInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(input.Value);

            // check round number of shared consensus, not term number
            if (consensusInformation.Round.RoundNumber <= State.MainChainRoundNumber.Value)
                return new Empty();
            Context.LogDebug(() => $"Shared miner list of round {consensusInformation.Round.RoundNumber}");
            var minersKeys = consensusInformation.Round.RealTimeMinersInformation.Keys;
            State.MainChainRoundNumber.Value = consensusInformation.Round.RoundNumber;
            State.MainChainCurrentMinerList.Value = new MinerList
            {
                Pubkeys = {minersKeys.Select(k => k.ToByteString())}
            };
            return new Empty();
        }

        #endregion
    }
}