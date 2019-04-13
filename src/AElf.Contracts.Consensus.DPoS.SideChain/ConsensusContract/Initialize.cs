using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
        public override Empty InitialConsensus(Round firstRound)
        {
            Assert(firstRound.RoundNumber == 1,
                ContractErrorCode.GetErrorMessage(ContractErrorCode.InvalidField, "Invalid round number."));

            Assert(firstRound.RealTimeMinersInformation.Any(),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.InvalidField, "No miner in input data."));

            InitialSettings(firstRound);

            Assert(TryToAddRoundInformation(firstRound),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed, "Failed to add round information."));

            return new Empty();
        }

        public override Empty ConfigStrategy(DPoSStrategyInput input)
        {
            Assert(!State.IsStrategyConfigured.Value,
                ContractErrorCode.GetErrorMessage(ContractErrorCode.InvalidOperation, "Already configured."));

            State.IsVerbose.Value = input.IsVerbose;

            State.IsStrategyConfigured.Value = true;

            LogVerbose("Consensus log level: Verbose");

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