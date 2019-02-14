using System.Collections.Generic;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Extensions
{
    public static class ConsensusExtensions
    {
        public static UInt64Value ToUInt64Value(this ulong value)
        {
            return new UInt64Value {Value = value};
        }

        public static StringValue ToStringValue(this string value)
        {
            return new StringValue {Value = value};
        }

        /// <summary>
        /// Include both min and max value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool InRange(this int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        public static Candidates ToCandidates(this IEnumerable<string> candidatesList)
        {
            return new Candidates {PublicKeys = {candidatesList}};
        }

        public static Miners ToMiners(this IEnumerable<string> minerPublicKeys)
        {
            return new Miners {PublicKeys = {minerPublicKeys}};
        }

        /// <summary>
        /// For calculating hash.
        /// </summary>
        /// <param name="votingRecord"></param>
        /// <returns></returns>
        public static VotingRecord ToSimpleRecord(this VotingRecord votingRecord)
        {
            return new VotingRecord
            {
                Count = votingRecord.Count,
                From = votingRecord.From,
                To = votingRecord.To,
                TermNumber = votingRecord.TermNumber,
                
            };
        }

        public static StringList ToStringList(this IEnumerable<string> list)
        {
            return new StringList {Values = {list}};
        }

        public static TicketsDictionary ToTicketsDictionary(this Dictionary<string, Tickets> dictionary)
        {
            var ticketsDictionary = new TicketsDictionary();
            foreach (var keyPair in dictionary)
            {
                ticketsDictionary.Maps.Add(keyPair.Key, keyPair.Value);
            }

            return ticketsDictionary;
        }
        
        public static TicketsDictionary ToTicketsDictionary(this IEnumerable<KeyValuePair<string, Tickets>> dictionary)
        {
            var ticketsDictionary = new TicketsDictionary();
            foreach (var keyPair in dictionary)
            {
                ticketsDictionary.Maps.Add(keyPair.Key, keyPair.Value);
            }

            return ticketsDictionary;
        }

        public static BlockAbstract GetAbstract(this IBlock block)
        {
            return new BlockAbstract
            {
                MinerPublicKey = block.Header.P.ToByteArray().ToHex(),
                Time = block.Header.Time
            };
        }
        
        public static bool IsSuccess(this BlockValidationResult result)
        {
            return (int) result < 11;
        }
    }
}