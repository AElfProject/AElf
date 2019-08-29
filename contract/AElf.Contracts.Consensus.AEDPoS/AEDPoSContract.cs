using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
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
            State.TreasuryContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            State.MaximumMinersCount.Value = int.MaxValue;

            State.LastIrreversibleBlockHeight.Value = 0;

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
            SetBlockchainStartTimestamp(input.GetStartTime());
            State.MiningInterval.Value = input.GetMiningInterval();
            SetMinerList(input.GetMinerList(), 1);

            if (!TryToAddRoundInformation(input))
            {
                Assert(false, "Failed to add round information.");
            }

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
                        Pubkey = publicKey,
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
            ProcessConsensusInformation(input);
            return new Empty();
        }

        #endregion

        #region NextRound

        public override Empty NextRound(Round input)
        {
            ProcessConsensusInformation(input);
            return new Empty();
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
            Context.LogDebug(() => $"Shared miner list of round {consensusInformation.Round.RoundNumber}");
            var minersKeys = consensusInformation.Round.RealTimeMinersInformation.Keys;
            State.MainChainRoundNumber.Value = consensusInformation.Round.RoundNumber;
            DistributeResourceTokensToPreviousMiners();
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
            foreach (var symbol in new List<string> {"RAM", "STO", "CPU", "NET"})
            {
                var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = Context.Self,
                    Symbol = symbol
                }).Balance;
                if (balance <= 0) continue;
                var amount = balance.Div(minerList.Count);
                foreach (var pubkey in minerList)
                {
                    var address = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(pubkey.ToHex()));
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

        public override Empty SetMaximumMinersCount(SInt32Value input)
        {
            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }

            var genesisOwnerAddress = State.ParliamentAuthContract.GetGenesisOwnerAddress.Call(new Empty());
            Assert(Context.Sender == genesisOwnerAddress, "No permission to set max miners count.");
            State.MaximumMinersCount.Value = input.Value;
            return new Empty();
        }

        #endregion
    }
}