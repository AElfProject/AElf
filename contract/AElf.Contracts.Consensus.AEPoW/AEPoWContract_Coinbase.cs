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
                State.SupposedProduceSeconds.Value = 10;
                State.CurrentDifficulty.Value = 1;
            }

            Assert(State.Records[Context.CurrentHeight] == null,
                $"Block of height {Context.CurrentHeight} already generated.");
            State.Records[Context.CurrentHeight] = new PoWRecord
            {
                Producer = input.Producer,
                Timestamp = Context.CurrentBlockTime
            };

            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            // TODO: Comment because ELF not initialized.
            // State.TokenContract.Transfer.Send(new TransferInput
            // {
            //     To = input.Producer,
            //     Amount = GetCurrentCoinBaseAmount(),
            //     Symbol = Context.Variables.NativeSymbol
            // });

            Context.Fire(new NonceUpdated
            {
                Nonce = input.Nonce,
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
            var averageProduceTime = timeSpans.Select(s => s.Seconds).Sum().Div(timeSpans.Count);
            Context.LogDebug(() => $"Average produce time: {averageProduceTime}");
            Context.LogDebug(() => $"Current difficulty: {State.CurrentDifficulty.Value}");
            State.CurrentDifficulty.Value = averageProduceTime > State.SupposedProduceSeconds.Value
                ? State.CurrentDifficulty.Value.Sub(1)
                : State.CurrentDifficulty.Value.Add(1);
        }
    }
}