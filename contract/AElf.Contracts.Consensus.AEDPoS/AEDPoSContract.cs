using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Contracts.TokenHolder;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract : AEDPoSContractImplContainer.AEDPoSContractImplBase
    {
        #region Initial

        /// <summary>
        /// The transaction with this method will generate on every node
        /// and executed with the same result.
        /// Otherwise, the block hash of the genesis block won't be equal.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty InitialAElfConsensusContract(InitialAElfConsensusContractInput input)
        {
            if (State.Initialized.Value) return new Empty();

            State.PeriodSeconds.Value = input.IsTermStayOne
                ? int.MaxValue
                : input.PeriodSeconds;

            State.MinerIncreaseInterval.Value = input.MinerIncreaseInterval;

            Context.LogDebug(() => $"There are {State.PeriodSeconds.Value} seconds per period.");

            if (input.IsSideChain)
            {
                InitialProfitSchemeForSideChain(input.PeriodSeconds);
            }

            if (input.IsTermStayOne || input.IsSideChain)
            {
                State.IsMainChain.Value = false;
                return new Empty();
            }

            State.IsMainChain.Value = true;

            State.ElectionContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            State.TreasuryContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            State.MaximumMinersCount.Value = int.MaxValue;

            if (State.TreasuryContract.Value != null)
            {
                State.TreasuryContract.UpdateMiningReward.Send(new Int64Value
                {
                    Value = AEDPoSContractConstants.InitialMiningRewardPerBlock
                });
            }

            return new Empty();
        }

        #endregion

        #region FirstRound

        /// <summary>
        /// The transaction with this method will generate on every node
        /// and executed with the same result.
        /// Otherwise, the block hash of the genesis block won't be equal.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty FirstRound(Round input)
        {
            /* Basic checks. */

            // Ensure the execution of the current method only happened
            // at the very beginning of the consensus process.
            if (State.CurrentRoundNumber.Value != 0) return new Empty();

            /* Initial settings. */
            State.CurrentTermNumber.Value = 1;
            State.CurrentRoundNumber.Value = 1;
            State.FirstRoundNumberOfEachTerm[1] = 1;
            State.MiningInterval.Value = input.GetMiningInterval();
            SetMinerList(input.GetMinerList(), 1);

            AddRoundInformation(input);

            Context.LogDebug(() =>
                $"Initial Miners: {input.RealTimeMinersInformation.Keys.Aggregate("\n", (key1, key2) => key1 + "\n" + key2)}");

            return new Empty();
        }

        #endregion

        #region UpdateValue

        public override Empty UpdateValue(UpdateValueInput input)
        {
            ProcessConsensusInformation(input);
            return new Empty();
        }

        #endregion

        #region UpdateTinyBlockInformation

        public override Empty UpdateTinyBlockInformation(TinyBlockInput input)
        {
            ProcessConsensusInformation(input);
            return new Empty();
        }

        #endregion

        #region NextRound

        public override Empty NextRound(Round input)
        {
            SupplyCurrentRoundInformation();
            ProcessConsensusInformation(input);
            return new Empty();
        }

        /// <summary>
        /// To fill up with InValue and Signature if some miners didn't mined during current round.
        /// </summary>
        private void SupplyCurrentRoundInformation()
        {
            var currentRound = GetCurrentRoundInformation(new Empty());
            Context.LogDebug(() => $"Before supply:\n{currentRound.ToString(Context.RecoverPublicKey().ToHex())}");
            var notMinedMiners = currentRound.RealTimeMinersInformation.Values.Where(m => m.OutValue == null).ToList();
            if (!notMinedMiners.Any()) return;
            TryToGetPreviousRoundInformation(out var previousRound);
            foreach (var miner in notMinedMiners)
            {
                Context.LogDebug(() => $"Miner pubkey {miner.Pubkey}");

                Hash previousInValue = null;
                Hash signature = null;

                // Normal situation: previous round information exists and contains this miner.
                if (previousRound != null && previousRound.RealTimeMinersInformation.ContainsKey(miner.Pubkey))
                {
                    // Check this miner's:
                    // 1. PreviousInValue in current round; (means previous in value recovered by other miners)
                    // 2. InValue in previous round; (means this miner hasn't produce blocks for a while)
                    previousInValue = currentRound.RealTimeMinersInformation[miner.Pubkey].PreviousInValue;
                    if (previousInValue == null)
                    {
                        previousInValue = previousRound.RealTimeMinersInformation[miner.Pubkey].InValue;
                    }

                    // If previousInValue is still null, treat this as abnormal situation.
                    if (previousInValue != null)
                    {
                        Context.LogDebug(() => $"Previous round: {previousRound.ToString(miner.Pubkey)}");
                        signature = previousRound.CalculateSignature(previousInValue);
                    }
                }

                if (previousInValue == null)
                {
                    // Handle abnormal situation.

                    // The fake in value shall only use once during one term.
                    previousInValue = HashHelper.ComputeFrom(miner);
                    signature = previousInValue;
                }

                // Fill this two fields at last.
                miner.InValue = previousInValue;
                miner.Signature = signature;

                currentRound.RealTimeMinersInformation[miner.Pubkey] = miner;
            }

            TryToUpdateRoundInformation(currentRound);
            Context.LogDebug(() => $"After supply:\n{currentRound.ToString(Context.RecoverPublicKey().ToHex())}");
        }

        #endregion

        #region UpdateConsensusInformation

        public override Empty UpdateConsensusInformation(ConsensusInformation input)
        {
            if (Context.Sender != Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName))
            {
                return new Empty();
            }

            Assert(!State.IsMainChain.Value, "Only side chain can update consensus information.");

            // For now we just extract the miner list from main chain consensus information, then update miners list.
            if (input == null || input.Value.IsEmpty) return new Empty();

            var consensusInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(input.Value);

            // check round number of shared consensus, not term number
            if (consensusInformation.Round.RoundNumber <= State.MainChainRoundNumber.Value)
                return new Empty();

            Context.LogDebug(() =>
                $"Shared miner list of round {consensusInformation.Round.RoundNumber}:" +
                $"{consensusInformation.Round.ToString("M")}");

            DistributeResourceTokensToPreviousMiners();

            State.MainChainRoundNumber.Value = consensusInformation.Round.RoundNumber;

            var minersKeys = consensusInformation.Round.RealTimeMinersInformation.Keys;
            State.MainChainCurrentMinerList.Value = new MinerList
            {
                Pubkeys = {minersKeys.Select(k => k.ToByteString())}
            };

            return new Empty();
        }

        private void DistributeResourceTokensToPreviousMiners()
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            var minerList = State.MainChainCurrentMinerList.Value.Pubkeys;
            foreach (var symbol in Context.Variables.GetStringArray(AEDPoSContractConstants.PayTxFeeSymbolListName)
                .Union(Context.Variables.GetStringArray(AEDPoSContractConstants.PayRentalSymbolListName)))
            {
                var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = Context.Self,
                    Symbol = symbol
                }).Balance;
                var amount = balance.Div(minerList.Count);
                Context.LogDebug(() => $"Consensus Contract {symbol} balance: {balance}. Every miner can get {amount}");
                if (amount <= 0) continue;
                foreach (var pubkey in minerList)
                {
                    var address = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(pubkey.ToHex()));
                    Context.LogDebug(() => $"Will send {amount} {symbol}s to {pubkey}");
                    State.TokenContract.Transfer.Send(new TransferInput
                    {
                        To = address,
                        Amount = amount,
                        Symbol = symbol
                    });
                }
            }
        }

        #endregion

        #region SetMaximumMinersCount

        public override Empty SetMaximumMinersCount(Int32Value input)
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            var genesisOwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            Assert(Context.Sender == genesisOwnerAddress, "No permission to set max miners count.");
            var currentLegalMinersCount = AEDPoSContractConstants.SupposedMinersCount.Add(
                (int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
                .Div(State.MinerIncreaseInterval.Value).Mul(2));
            // TODO: Add this judgement because this can cause consensus header information getting problem after changing term. Consider remove this limitation.
            Assert(input.Value >= currentLegalMinersCount,
                $"Maximum miners count cannot less than {currentLegalMinersCount}");
            State.MaximumMinersCount.Value = input.Value;
            return new Empty();
        }

        #endregion

        public override Hash GetRandomHash(Int64Value input)
        {
            Assert(input.Value > 1, "Invalid block height.");
            Assert(Context.CurrentHeight >= input.Value, "Block height not reached.");
            return State.RandomHashes[input.Value] ?? Hash.Empty;
        }
    }
}