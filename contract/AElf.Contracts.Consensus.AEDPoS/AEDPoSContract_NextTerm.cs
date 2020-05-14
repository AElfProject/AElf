using System.Linq;
using AElf.Contracts.Election;
using Acs10;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public override Empty NextTerm(Round input)
        {
            SupplyCurrentRoundInformation();
            ProcessConsensusInformation(input);
            return new Empty();
        }

        private void UpdateProducedBlocksNumberOfSender(Round input)
        {
            var senderPubkey = Context.RecoverPublicKey().ToHex();

            // Update produced block number of transaction sender.
            if (input.RealTimeMinersInformation.ContainsKey(senderPubkey))
            {
                input.RealTimeMinersInformation[senderPubkey].ProducedBlocks =
                    input.RealTimeMinersInformation[senderPubkey].ProducedBlocks.Add(1);
            }
            else
            {
                // If the sender isn't in miner list of next term.
                State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
                {
                    Pubkey = senderPubkey,
                    RecentlyProducedBlocks = 1
                });
            }
        }

        private void UpdateCurrentMinerInformationToElectionContract(Round previousRound)
        {
            State.ElectionContract.UpdateMultipleCandidateInformation.Send(new UpdateMultipleCandidateInformationInput
            {
                Value =
                {
                    previousRound.RealTimeMinersInformation.Select(i => new UpdateCandidateInformationInput
                    {
                        Pubkey = i.Key,
                        RecentlyProducedBlocks = i.Value.ProducedBlocks,
                        RecentlyMissedTimeSlots = i.Value.MissedTimeSlots
                    })
                }
            });
        }

        private void UpdateMinersCountToElectionContract(Round input)
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

        /// <summary>
        /// Only Main Chain can perform this action.
        /// </summary>
        /// <param name="minerList"></param>
        /// <param name="termNumber"></param>
        /// <param name="gonnaReplaceSomeone"></param>
        /// <returns></returns>
        private bool SetMinerList(MinerList minerList, long termNumber, bool gonnaReplaceSomeone = false)
        {
            // Miners for one specific term should only update once.
            var minerListFromState = State.MinerListMap[termNumber];
            if (gonnaReplaceSomeone || minerListFromState == null)
            {
                State.MainChainCurrentMinerList.Value = minerList;
                State.MinerListMap[termNumber] = minerList;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Normally this process contained in NextRound method.
        /// </summary>
        private void CountMissedTimeSlots()
        {
            if (!TryToGetCurrentRoundInformation(out var currentRound)) return;

            foreach (var minerInRound in currentRound.RealTimeMinersInformation)
            {
                if (minerInRound.Value.OutValue == null)
                {
                    minerInRound.Value.MissedTimeSlots = minerInRound.Value.MissedTimeSlots.Add(1);
                }
            }

            TryToUpdateRoundInformation(currentRound);
        }

        private bool TryToUpdateTermNumber(long termNumber)
        {
            var oldTermNumber = State.CurrentTermNumber.Value;
            if (termNumber != 1 && oldTermNumber + 1 != termNumber)
            {
                return false;
            }

            State.CurrentTermNumber.Value = termNumber;
            return true;
        }

        private bool DonateMiningReward(Round previousRound)
        {
            if (State.TreasuryContract.Value == null)
            {
                var treasuryContractAddress =
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
                // Return false if Treasury Contract didn't deployed.
                if (treasuryContractAddress == null) return false;
                State.TreasuryContract.Value = treasuryContractAddress;
            }

            var miningRewardPerBlock = GetMiningRewardPerBlock();
            var amount = previousRound.GetMinedBlocks().Mul(miningRewardPerBlock);
            State.TreasuryContract.UpdateMiningReward.Send(new Int64Value {Value = miningRewardPerBlock});

            if (amount > 0)
            {
                State.TreasuryContract.Donate.Send(new DonateInput
                {
                    Symbol = Context.Variables.NativeSymbol,
                    Amount = amount
                });

                Context.Fire(new MiningRewardGenerated
                {
                    TermNumber = previousRound.TermNumber,
                    Amount = amount
                });
            }

            Context.LogDebug(() => $"Released {amount} mining rewards.");

            return true;
        }

        private long GetMiningRewardPerBlock()
        {
            var miningReward = AEDPoSContractConstants.InitialMiningRewardPerBlock;
            var blockAge = GetBlockchainAge();
            var denominator = blockAge.Div(AEDPoSContractConstants.TimeToReduceMiningRewardByHalf);
            for (var i = 0; i < denominator; i++)
            {
                miningReward = miningReward.Div(2);
            }

            return miningReward;
        }
    }
}