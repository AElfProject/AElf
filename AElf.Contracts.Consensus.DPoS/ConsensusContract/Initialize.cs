using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract
    {
        public override Empty InitialDPoSContract(InitialDPoSContractInput input)
        {
            Assert(!State.Initialized.Value,
                ContractErrorCode.GetErrorMessage(ContractErrorCode.InvalidOperation, "Already initialized."));

            State.TokenContractSystemName.Value = input.TokenContractSystemName;
            State.DividendContractSystemName.Value = input.DividendsContractSystemName;

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Empty ConfigStrategy(DPoSStrategyInput input)
        {
            Assert(!State.IsStrategyConfigured.Value,
                ContractErrorCode.GetErrorMessage(ContractErrorCode.InvalidOperation, "Already configured."));

            State.IsBlockchainAgeSettable.Value = input.IsBlockchainAgeSettable;
            State.IsTimeSlotSkippable.Value = input.IsTimeSlotSkippable;
            State.IsVerbose.Value = input.IsVerbose;

            State.IsStrategyConfigured.Value = true;

            LogVerbose("Consensus log level: Verbose");
            LogVerbose($"Is blockchain age settable: {input.IsBlockchainAgeSettable}");
            LogVerbose($"Is time slot skippable: {input.IsTimeSlotSkippable}");

            return new Empty();
        }

        public override Empty InitialConsensus(Round firstRound)
        {
            Assert(firstRound.RoundNumber == 1,
                ContractErrorCode.GetErrorMessage(ContractErrorCode.InvalidField, "Invalid round number."));

            Assert(firstRound.RealTimeMinersInformation.Any(),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.InvalidField, "No miner in input data."));

            InitialSettings(firstRound);

            Assert(TryToAddRoundInformation(firstRound),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed, "Failed to add round information."));

            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();

            return new Empty();
        }

        /// <summary>
        /// Initial miners can set blockchain age manually.
        /// For testing.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty SetBlockchainAge(SInt64Value input)
        {
            Assert(TryToGetRoundInformation(1, out var firstRound),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.NotFound, "Failed to get first round information"));
            Assert(firstRound.RealTimeMinersInformation.Keys.Contains(Context.RecoverPublicKey().ToHex()),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.NoPermission,
                    "No permission to change blockchain age."));
            // Don't use `UpdateBlockchainAge` here. Because in testing, we can set blockchain age for free.
            State.AgeField.Value = input.Value;

            LogVerbose($"{Context.RecoverPublicKey().ToHex()} set blockchain age to {input.Value}");

            return new Empty();
        }

        private void InitialSettings(Round firstRound)
        {
            State.CurrentTermNumberField.Value = 1;
            State.CurrentRoundNumberField.Value = 1;
            State.AgeField.Value = 1;
            State.TermToFirstRoundMap[1L.ToInt64Value()] = 1L.ToInt64Value();
            SetBlockchainStartTimestamp(firstRound.GetStartTime().ToTimestamp());
            State.MiningIntervalField.Value = firstRound.GetMiningInterval();

            SetInitialMinersAliases(firstRound.RealTimeMinersInformation.Keys);

            var miners = firstRound.RealTimeMinersInformation.Keys.ToList().ToMiners(1);
            miners.TermNumber = 1;
            SetMiners(miners);
        }
    }
}