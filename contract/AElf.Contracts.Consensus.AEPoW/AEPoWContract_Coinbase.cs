using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract
    {
        public override Empty CoinBase(CoinBaseInput input)
        {
            if (Context.CurrentHeight == 2)
            {
                State.BlockchainStartTime.Value = Context.CurrentBlockTime;
                State.SupposedProduceNanoSeconds.Value = 10000000000;
                State.CurrentDifficulty.Value = 1;
            }

            Assert(State.Records[Context.CurrentHeight] == null,
                $"Block of height {Context.CurrentHeight} already generated.");
            Context.LogDebug(() => $"Record of height {Context.CurrentHeight} set.");
            State.Records[Context.CurrentHeight] = new PoWRecord
            {
                Producer = Context.Sender,
                Timestamp = Context.CurrentBlockTime,
                NonceNumber = input.NonceNumber
            };

            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            // TODO: ELF not initialized in PoW Chain.
            // State.TokenContract.Transfer.Send(new TransferInput
            // {
            //     To = Context.Sender,
            //     Amount = GetCurrentCoinBaseAmount(),
            //     Symbol = Context.Variables.NativeSymbol
            // });

            Context.Fire(new NonceUpdated
            {
                NonceNumber = input.NonceNumber,
                BlockHeight = Context.CurrentHeight
            });

            AdjustDifficulty();

            return new Empty();
        }

        // TODO: Calculated based on blockchain start time.
        private long GetCurrentCoinBaseAmount()
        {
            return 1_00000000;
        }

        /// <summary>
        /// Adjust difficulty according to timestamps of State.Records
        /// </summary>
        private void AdjustDifficulty()
        {
            Context.LogDebug(() => $"Entered AdjustDifficulty.");
            var currentHeight = Context.CurrentHeight;
            if (currentHeight < AdjustDifficultyReferenceBlockNumber) return;
            var timeSpans = new List<Duration>();
            var tempBlockTime = Context.CurrentBlockTime;
            for (var i = 1; i < AdjustDifficultyReferenceBlockNumber; i++)
            {
                var height = currentHeight.Sub(i);
                var record = State.Records[height];
                if (record == null) continue;
                timeSpans.Add(tempBlockTime - record.Timestamp);
                tempBlockTime = record.Timestamp;
            }

            // Cannot use Average method directly because safe checks of smart contract.
            var averageProduceTime = timeSpans.Select(s => s.Nanos.Div(timeSpans.Count)).Sum();
            Context.LogDebug(() => $"Average produce time: {averageProduceTime} nanos");
            Context.LogDebug(() => $"Current difficulty: {State.CurrentDifficulty.Value}");
            State.CurrentDifficulty.Value = averageProduceTime > State.SupposedProduceNanoSeconds.Value
                ? State.CurrentDifficulty.Value.Sub(1)
                : State.CurrentDifficulty.Value.Add(1);
        }
    }
}