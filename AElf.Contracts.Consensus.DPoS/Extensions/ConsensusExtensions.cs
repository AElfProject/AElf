using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.DPoS.Extensions
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

        public static Miners ToMiners(this IEnumerable<string> minerPublicKeys, ulong termNumber = 1)
        {
            return new Miners
            {
                PublicKeys = {minerPublicKeys},
                TermNumber = termNumber
            };
        }
    }
}