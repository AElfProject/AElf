using System.Linq;
using System.Text;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public string GetLogs(string publicKey, AElfConsensusBehaviour behaviour)
        {
            var logs = new StringBuilder($"\n[Round {RoundNumber}](Round Id: {RoundId})[Term {TermNumber}]");
            foreach (var minerInRound in RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var minerInformation = new StringBuilder("\n");
                minerInformation.Append($"[{minerInRound.Pubkey.Substring(0, 10)}]");
                minerInformation.Append(minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "");
                minerInformation.AppendLine(minerInRound.Pubkey == publicKey
                    ? "(This Node)"
                    : "");
                minerInformation.AppendLine($"Order:\t {minerInRound.Order}");
                minerInformation.AppendLine(
                    $"Expect:\t {minerInRound.ExpectedMiningTime?.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff}");
                var roundStartTime = GetStartTime();
                var actualMiningTimes = minerInRound.ActualMiningTimes.OrderBy(t => t).Select(t =>
                {
                    if (t < roundStartTime)
                    {
                        return $"{t.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff} (For Extra Block Slot Of Previous Round)";
                    }

                    return t.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd HH.mm.ss,ffffff");
                });
                var actualMiningTimesStr = minerInRound.ActualMiningTimes.Any() ? string.Join("\n\t ", actualMiningTimes) : "";
                minerInformation.AppendLine($"Actual:\t {actualMiningTimesStr}");
                minerInformation.AppendLine($"Out:\t {minerInRound.OutValue?.ToHex()}");
                if (RoundNumber != 1)
                {
                    minerInformation.AppendLine($"PreIn:\t {minerInRound.PreviousInValue?.ToHex()}");
                }

                minerInformation.AppendLine($"Sig:\t {minerInRound.Signature?.ToHex()}");
                minerInformation.AppendLine($"Mine:\t {minerInRound.ProducedBlocks}");
                minerInformation.AppendLine($"Miss:\t {minerInRound.MissedTimeSlots}");
                minerInformation.AppendLine($"Tiny:\t {minerInRound.ProducedTinyBlocks}");
                minerInformation.AppendLine($"NOrder:\t {minerInRound.FinalOrderOfNextRound}");
                minerInformation.AppendLine($"Lib:\t {minerInRound.ImpliedIrreversibleBlockHeight}");

                logs.Append(minerInformation);
            }

            logs.AppendLine($"Recent behaviour: {behaviour.ToString()}");

            return logs.ToString();
        }
    }
}