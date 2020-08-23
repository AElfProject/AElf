using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContract
    {
        private bool IsValid(Hash resultHash)
        {
            return true;
        }

        private long CalculateAverageProduceMilliseconds()
        {
            Context.LogDebug(() => $"Entered CalculateAverageProduceMilliseconds.");
            var currentHeight = Context.CurrentHeight;
            if (currentHeight < AdjustDifficultyReferenceBlockNumber) return State.SupposedProduceMilliseconds.Value;
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
            var averageProduceTime = timeSpans.Select(s => s.Nanos.Div(timeSpans.Count).Div(1000000)).Sum();
            Context.LogDebug(() => $"Average produce time: {averageProduceTime} milliseconds.");

            return averageProduceTime;
        }
    }
}