using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract : ElectionContractContainer.ElectionContractBase
    {
        /// <summary>
        /// Initialize the ElectionContract and corresponding contract states.
        /// </summary>
        /// <param name="input">InitialElectionContractInput</param>
        /// <returns></returns>
        public override Empty InitialElectionContract(InitialElectionContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.Candidates.Value = new PubkeyList();
            State.BlackList.Value = new PubkeyList();

            State.MinimumLockTime.Value = input.MinimumLockTime;
            State.MaximumLockTime.Value = input.MaximumLockTime;

            State.TimeEachTerm.Value = input.TimeEachTerm;

            State.MinersCount.Value = input.MinerList.Count;
            State.InitialMiners.Value = new PubkeyList
            {
                Value = {input.MinerList.Select(k => k.ToByteString())}
            };
            foreach (var pubkey in input.MinerList)
            {
                State.CandidateInformationMap[pubkey] = new CandidateInformation
                {
                    Pubkey = pubkey
                };
            }

            State.CurrentTermNumber.Value = 1;

            State.DataCentersRankingList.Value = new DataCenterRankingList();

            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty RegisterElectionVotingEvent(Empty input)
        {
            Assert(!State.VotingEventRegistered.Value, "Already registered.");

            State.VoteContract.Value = Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName);

            var votingRegisterInput = new VotingRegisterInput
            {
                IsLockToken = false,
                AcceptedCurrency = Context.Variables.NativeSymbol,
                TotalSnapshotNumber = long.MaxValue,
                StartTimestamp = TimestampHelper.MinValue,
                EndTimestamp = TimestampHelper.MaxValue
            };
            State.VoteContract.Register.Send(votingRegisterInput);

            State.MinerElectionVotingItemId.Value = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(votingRegisterInput),
                HashHelper.ComputeFrom(Context.Self));

            State.VotingEventRegistered.Value = true;
            return new Empty();
        }

        #region TakeSnapshot

        public override Empty TakeSnapshot(TakeElectionSnapshotInput input)
        {
            if (State.AEDPoSContract.Value == null)
            {
                State.AEDPoSContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }

            Assert(State.AEDPoSContract.Value == Context.Sender, "No permission.");

            SavePreviousTermInformation(input);

            if (State.ProfitContract.Value == null)
            {
                var profitContractAddress =
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
                // Return if profit contract didn't deployed. (Often in test cases.)
                if (profitContractAddress == null) return new Empty();
                State.ProfitContract.Value = profitContractAddress;
            }

            // Update snapshot of corresponding voting record by the way.
            State.VoteContract.TakeSnapshot.Send(new TakeSnapshotInput
            {
                SnapshotNumber = input.TermNumber,
                VotingItemId = State.MinerElectionVotingItemId.Value
            });

            State.CurrentTermNumber.Value = input.TermNumber.Add(1);

            var previousMiners = State.AEDPoSContract.GetPreviousRoundInformation.Call(new Empty())
                .RealTimeMinersInformation.Keys.ToList();

            foreach (var pubkey in previousMiners)
            {
                UpdateCandidateInformation(pubkey, input.TermNumber, previousMiners);
            }

            if (State.TreasuryContract.Value == null)
            {
                State.TreasuryContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
            }

            var symbolList = State.TreasuryContract.GetSymbolList.Call(new Empty());
            var amountsMap = symbolList.Value.ToDictionary(s => s, s => 0L);
            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.SubsidyHash.Value,
                Period = input.TermNumber,
                AmountsMap = {amountsMap}
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.WelfareHash.Value,
                Period = input.TermNumber,
                AmountsMap = {amountsMap}
            });

            return new Empty();
        }

        private void SavePreviousTermInformation(TakeElectionSnapshotInput input)
        {
            var snapshot = new TermSnapshot
            {
                MinedBlocks = input.MinedBlocks,
                EndRoundNumber = input.RoundNumber
            };

            if (State.Candidates.Value == null) return;

            foreach (var pubkey in State.Candidates.Value.Value)
            {
                var votes = State.CandidateVotes[pubkey.ToHex()];
                var validObtainedVotesAmount = 0L;
                if (votes != null)
                {
                    validObtainedVotesAmount = votes.ObtainedActiveVotedVotesAmount;
                }

                snapshot.ElectionResult.Add(pubkey.ToHex(), validObtainedVotesAmount);
            }

            State.Snapshots[input.TermNumber] = snapshot;
        }

        private void UpdateCandidateInformation(string pubkey, long lastTermNumber,
            List<string> previousMiners)
        {
            var candidateInformation = State.CandidateInformationMap[pubkey];
            if (candidateInformation == null) return;
            candidateInformation.Terms.Add(lastTermNumber);
            var victories = GetVictories(previousMiners);
            candidateInformation.ContinualAppointmentCount = victories.Contains(pubkey.ToByteString())
                ? candidateInformation.ContinualAppointmentCount.Add(1)
                : 0;
            State.CandidateInformationMap[pubkey] = candidateInformation;
        }

        #endregion

        /// <summary>
        /// Update the candidate information,if it's not evil node.
        /// </summary>
        /// <param name="input">UpdateCandidateInformationInput</param>
        /// <returns></returns>
        public override Empty UpdateCandidateInformation(UpdateCandidateInformationInput input)
        {
            Assert(
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName) == Context.Sender,
                "Only consensus contract can update candidate information.");

            var candidateInformation = State.CandidateInformationMap[input.Pubkey];
            if (candidateInformation == null)
            {
                return new Empty();
            }

            if (input.IsEvilNode)
            {
                var publicKeyByte = ByteArrayHelper.HexStringToByteArray(input.Pubkey);
                State.BlackList.Value.Value.Add(ByteString.CopyFrom(publicKeyByte));
                if (State.ProfitContract.Value == null)
                    State.ProfitContract.Value =
                        Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
                State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
                {
                    SchemeId = State.SubsidyHash.Value,
                    Beneficiary = Address.FromPublicKey(publicKeyByte)
                });
                Context.LogDebug(() => $"Marked {input.Pubkey.Substring(0, 10)} as an evil node.");
                State.CandidateInformationMap.Remove(input.Pubkey);
                var candidates = State.Candidates.Value;
                candidates.Value.Remove(ByteString.CopyFrom(publicKeyByte));
                State.Candidates.Value = candidates;
                return new Empty();
            }

            candidateInformation.ProducedBlocks = candidateInformation.ProducedBlocks.Add(input.RecentlyProducedBlocks);
            candidateInformation.MissedTimeSlots =
                candidateInformation.MissedTimeSlots.Add(input.RecentlyMissedTimeSlots);
            State.CandidateInformationMap[input.Pubkey] = candidateInformation;
            return new Empty();
        }

        public override Empty UpdateMultipleCandidateInformation(UpdateMultipleCandidateInformationInput input)
        {
            Assert(
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName) == Context.Sender,
                "Only consensus contract can update candidate information.");

            foreach (var updateCandidateInformationInput in input.Value)
            {
                UpdateCandidateInformation(updateCandidateInformationInput);
            }

            return new Empty();
        }

        public override Empty UpdateMinersCount(UpdateMinersCountInput input)
        {
            Context.LogDebug(() =>
                $"Consensus Contract Address: {Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName)}");
            Context.LogDebug(() => $"Sender Address: {Context.Sender}");
            Assert(
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName) == Context.Sender,
                "Only consensus contract can update miners count.");
            State.MinersCount.Value = input.MinersCount;
            return new Empty();
        }

        public override Empty SetTreasurySchemeIds(SetTreasurySchemeIdsInput input)
        {
            Assert(State.TreasuryHash.Value == null, "Treasury profit ids already set.");
            State.TreasuryHash.Value = input.TreasuryHash;
            State.WelfareHash.Value = input.WelfareHash;
            State.SubsidyHash.Value = input.SubsidyHash;
            State.ReElectionRewardHash.Value = input.ReElectionRewardHash;
            State.VotesRewardHash.Value = input.VotesRewardHash;
            return new Empty();
        }
    }
}