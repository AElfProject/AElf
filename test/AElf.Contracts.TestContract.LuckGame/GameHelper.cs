using System;
using System.Globalization;
using AElf.Types;

namespace AElf.Contracts.TestContract.LuckGame
{
    public partial class LuckGameContract
    {
        private Hash GenerateBetId(BetInput input)
        {
            var hash1 = Context.TransactionId;
            var hash2 = Hash.FromMessage(input);

            return Hash.FromTwoHashes(hash1, hash2);
        }

        private long GenerateRandomNumber(Hash hash, int min, int max)
        {
            if(min >= max)
                Assert(false, $"Invalid random number range from ({min}, {max}).");
            
            var value = hash.ToInt64();

            return value % (max + 1 - min) + min;
        }

        private int GetOdds(long betAmount, BetType type)
        {
            if(betAmount < 6 || betAmount > 94)
                Assert(false, "Invalid bet number, number should in range (6, 94).");
            Assert(type != BetType.Invalid, "Invalid bet type, only Small or Big permitted.");
            
            if (type.Equals(BetType.Small))
            {
                var rate = Math.Pow(betAmount, -1.083) * 13923;
                return int.Parse(rate.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                var rate = Math.Pow(100 - betAmount, -1.083) * 13923;
                return int.Parse(rate.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}