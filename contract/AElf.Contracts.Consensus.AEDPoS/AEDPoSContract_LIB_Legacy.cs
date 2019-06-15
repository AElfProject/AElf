using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
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
            offset = offset.Mul(AEDPoSContractConstants.TinyBlocksNumber);
            Context.LogDebug(() => $"LIB found, offset is {offset}");
            Context.Fire(new IrreversibleBlockFound
            {
                Offset = offset
            });
        }

        private bool CalculateLastIrreversibleBlock(out long offset)
        {
            offset = 0;
            if (!TryToGetCurrentRoundInformation(out var currentRound) || currentRound.RoundNumber <= 2 ||
                !IsMinersOnSameBranchEnough(out var publicKeys, out var isCurrentRoundEnough)) return false;
            var minersCount = currentRound.RealTimeMinersInformation.Count;
            offset = isCurrentRoundEnough ? minersCount.Mul(2) : minersCount.Mul(3);
            return isCurrentRoundEnough
                ? AreMinersOnSameBranchInCertainRound(publicKeys, currentRound.RoundNumber.Sub(1))
                : AreMinersOnSameBranchInCertainRound(publicKeys, currentRound.RoundNumber.Sub(2));
        }

        private bool AreMinersOnSameBranchInCertainRound(IEnumerable<string> publicKeys, long roundNumber)
        {
            if (!TryToGetRoundInformation(roundNumber, out var round)) return false;
            var validMiners = round.RealTimeMinersInformation.Values.Where(i => i.OutValue != null).Select(i => i.PublicKey);
            return publicKeys.All(k => validMiners.Contains(k));
        }

        private bool IsMinersOnSameBranchEnough(out HashSet<string> publicKeys, out bool isCurrentRoundEnough)
        {
            publicKeys = new HashSet<string>();
            isCurrentRoundEnough = true;
            
            if (!TryToGetCurrentRoundInformation(out var currentRound)) return false;

            var currentRoundMiners = currentRound.RealTimeMinersInformation;

            var minersCount = currentRoundMiners.Count;

            var minimumCount = minersCount.Mul(2).Div(3).Add(1);

            if (minersCount == 1)
            {
                // Single node will set every previous block as LIB.
                return true;
            }

            var validMinersOfCurrentRound = currentRoundMiners.Values.Where(m => m.OutValue != null).ToList();
            var validMinersCountOfCurrentRound = validMinersOfCurrentRound.Count;

            if (validMinersCountOfCurrentRound >= minimumCount)
            {
                return true;
            }

            // Current round is not enough to find LIB.
            isCurrentRoundEnough = false;
            publicKeys = new HashSet<string>(validMinersOfCurrentRound.Select(m => m.PublicKey));

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
                        return true;
                    }
                }
            }

            return false;
        }
    }
}