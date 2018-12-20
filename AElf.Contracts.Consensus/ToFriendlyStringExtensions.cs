using System.Collections.Generic;
using AElf.Kernel;
using Newtonsoft.Json.Linq;

namespace AElf.Contracts.Consensus
{
    public static class ToFriendlyStringExtensions
    {
        public static string ToFriendlyString(this CandidateInHistory origin)
        {
            var jObject = new JObject
            {
                ["Terms"] = origin.Terms.ToAString(),
                ["ProducedBlocks"] = origin.ProducedBlocks,
                ["MissedTimeSlots"] = origin.MissedTimeSlots,
                ["ContinualAppointmentCount"] = origin.ContinualAppointmentCount,
                ["ReappointmentCount"] = origin.ReappointmentCount,
                ["Aliases"] = origin.Aliases.ToAString(),
            };

            return jObject.ToString();
        }

        public static string ToFriendlyString(this VotingRecord origin)
        {
            var jObject = new JObject
            {
                ["From"] = origin.From,
                ["To"] = origin.To,
                ["Count"] = origin.Count,
                ["RoundNumber"] = origin.RoundNumber,
                ["TransactionId"] = origin.TransactionId.ToHex(),
                ["VoteAge"] = origin.VoteAge,
                ["LockDaysList"] = origin.LockDaysList.ToAString(),
                ["UnlockAge"] = origin.UnlockAge,
                ["TermNumber"] = origin.TermNumber,
            };

            return jObject.ToString();
        }

        public static string ToFriendlyString(this Tickets origin)
        {
            var recordString = new List<string>();
            foreach (var votingRecord in origin.VotingRecords)
            {
                recordString.Add(votingRecord.ToFriendlyString());
            }
            
            var jObject = new JObject
            {
                ["TotalTickets"] = origin.TotalTickets,
                ["VotingRecords"] = recordString.ToAString()
            };

            return jObject.ToString();
        }

        public static string ToFriendlyString(this TermSnapshot origin)
        {
            var candidatesSnapshot = new List<string>();
            foreach (var candidateInTerm in origin.CandidatesSnapshot)
            {
                candidatesSnapshot.Add(candidateInTerm.ToFriendlyString());
            }
            
            var jObject = new JObject
            {
                ["EndRoundNumber"] = origin.EndRoundNumber,
                ["TotalBlocks"] = origin.TotalBlocks,
                ["TermNumber"] = origin.TermNumber,
                ["CandidatesSnapshot"] = candidatesSnapshot.ToAString(),
            };

            return jObject.ToString();
        }

        public static string ToFriendlyString(this CandidateInTerm origin)
        {
            var jObject = new JObject
            {
                ["PublicKey"] = origin.PublicKey,
                ["Votes"] = origin.Votes,
            };

            return jObject.ToString();
        }
    }
}