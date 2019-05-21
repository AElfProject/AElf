using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// Basically this method only used for testing LIB finding logic.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value GetLIBOffset(Empty input)
        {
            return new SInt64Value {Value = CalculateLastIrreversibleBlock(out var offset) ? offset : 0};
        }

        private void TryToFindLastIrreversibleBlock()
        {
            if (!CalculateLastIrreversibleBlock(out var offset)) return;
            Context.LogDebug(() => $"LIB found, offset is {offset}");
            Context.Fire(new IrreversibleBlockFound
            {
                Offset = offset.Mul(AEDPoSContractConstants.TinyBlocksNumber)
            });
        }

        private bool CalculateLastIrreversibleBlock(out long offset)
        {
            offset = 0;

            if (!TryToGetCurrentRoundInformation(out var currentRound)) return false;

            var currentRoundMiners = currentRound.RealTimeMinersInformation;

            var minersCount = currentRoundMiners.Count;

            var minimumCount = ((int) ((minersCount * 2d) / 3)) + 1;

            if (minersCount == 1)
            {
                // Single node will set every previous block as LIB.
                offset = 1;
                return true;
            }

            var validMinersOfCurrentRound = currentRoundMiners.Values.Where(m => m.OutValue != null).ToList();
            var validMinersCountOfCurrentRound = validMinersOfCurrentRound.Count;

            if (validMinersCountOfCurrentRound >= minimumCount)
            {
                offset = minimumCount;
                return true;
            }

            // Current round is not enough to find LIB.

            var publicKeys = new HashSet<string>(validMinersOfCurrentRound.Select(m => m.PublicKey));

            if (TryToGetPreviousRoundInformation(out var previousRound))
            {
                var preRoundMiners = previousRound.RealTimeMinersInformation.Values.OrderByDescending(m => m.Order)
                    .Select(m => m.PublicKey).ToList();

                var traversalBlocksCount = publicKeys.Count;

                for (var i = 0; i < minersCount; i++)
                {
                    if (++traversalBlocksCount > minersCount)
                    {
                        return false;
                    }

                    var miner = preRoundMiners[i];

                    if (previousRound.RealTimeMinersInformation[miner].OutValue != null)
                    {
                        if (!publicKeys.Contains(miner))
                            publicKeys.Add(miner);
                    }

                    if (publicKeys.Count >= minimumCount)
                    {
                        offset = minimumCount;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}