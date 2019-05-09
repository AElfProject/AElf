using System.Linq;
using System.Text;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    internal partial class Round
    {
        public string GetLogs(string publicKey, AElfConsensusBehaviour behaviour)
        {
            var logs = new StringBuilder($"\n[Round {RoundNumber}](Round Id: {RoundId})[Term {TermNumber}]");
            foreach (var minerInRound in RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var minerInformation = new StringBuilder("\n");
                minerInformation.Append($"[{minerInRound.PublicKey.Substring(0, 10)}]");
                minerInformation.Append(minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "");
                minerInformation.AppendLine(minerInRound.PublicKey == publicKey
                    ? "(This Node)"
                    : "");
                minerInformation.AppendLine($"Order:\t {minerInRound.Order}");
                minerInformation.AppendLine(
                    $"Expect:\t {minerInRound.ExpectedMiningTime?.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff}");
                minerInformation.AppendLine(
                    $"Actual:\t {minerInRound.ActualMiningTime?.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff}");
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

                logs.Append(minerInformation);
            }

            logs.AppendLine($"Recent behaviour: {behaviour.ToString()}");

            return logs.ToString();
        }
    }
}