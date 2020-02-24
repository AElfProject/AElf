using System;
using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round : IFormattable
    {
        private string GetLogs(string publicKey)
        {
            var logs = $"\n[Round {RoundNumber}](Round Id: {RoundId})[Term {TermNumber}]";
            foreach (var minerInRound in RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var minerInformation = "\n";
                minerInformation = minerInformation.Append($"[{minerInRound.Pubkey.Substring(0, 10)}]");
                minerInformation = minerInformation.Append(minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "");
                minerInformation = minerInformation.AppendLine(minerInRound.Pubkey == publicKey
                    ? "(This Node)"
                    : "");
                minerInformation = minerInformation.AppendLine($"Order:\t {minerInRound.Order}");
                minerInformation = minerInformation.AppendLine(
                    $"Expect:\t {minerInRound.ExpectedMiningTime?.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff}");
                var roundStartTime = GetRoundStartTime();
                var actualMiningTimes = minerInRound.ActualMiningTimes.OrderBy(t => t).Select(t =>
                {
                    if (t < roundStartTime)
                    {
                        return
                            $"{t.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff} (For Extra Block Slot Of Previous Round)";
                    }

                    return t.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd HH.mm.ss,ffffff");
                });
                var actualMiningTimesStr =
                    minerInRound.ActualMiningTimes.Any() ? string.Join("\n\t ", actualMiningTimes) : "";
                minerInformation = minerInformation.AppendLine($"Actual:\t {actualMiningTimesStr}");
                minerInformation = minerInformation.AppendLine($"Out:\t {minerInRound.OutValue?.ToHex()}");
                if (RoundNumber != 1)
                {
                    minerInformation = minerInformation.AppendLine($"PreIn:\t {minerInRound.PreviousInValue?.ToHex()}");
                }

                minerInformation = minerInformation.AppendLine($"In:\t {minerInRound.InValue?.ToHex()}");
                minerInformation = minerInformation.AppendLine($"Sig:\t {minerInRound.Signature?.ToHex()}");
                minerInformation = minerInformation.AppendLine($"Mine:\t {minerInRound.ProducedBlocks}");
                minerInformation = minerInformation.AppendLine($"Miss:\t {minerInRound.MissedTimeSlots}");
                minerInformation = minerInformation.AppendLine($"Tiny:\t {minerInRound.ActualMiningTimes.Count}");
                minerInformation = minerInformation.AppendLine($"NOrder:\t {minerInRound.FinalOrderOfNextRound}");
                minerInformation = minerInformation.AppendLine($"Lib:\t {minerInRound.ImpliedIrreversibleBlockHeight}");

                logs = logs.Append(minerInformation);
            }

            return logs;
        }

        public string ToString(string format, IFormatProvider formatProvider = null)
        {
            if (string.IsNullOrEmpty(format)) format = "G";

            switch (format)
            {
                case "G": return ToString();
                case "M":
                    // Return formatted miner list.
                    return RealTimeMinersInformation.Keys.Aggregate("\n", (key1, key2) => key1 + "\n" + key2);
            }

            return GetLogs(format);
        }
    }
}