using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace AElf.Common
{
    public static class ChainHelpers
    {
        public static readonly int ChainIdLength = GlobalConfig.ChainIdLength;
        public static byte[] GetRandomChainId()
        {
            Random r = new Random();
            BigInteger randomBigInt = r.Next(198535, 11316496);
            var bigIntByteArray = randomBigInt.ToByteArray();
            
            var randomBytes = new byte[ChainIdLength];
            for (int i = 1; i <= ChainIdLength; i++)
            {
                randomBytes[i - 1] = bigIntByteArray[ChainIdLength - i];
            }

            return randomBytes;
        }

        public static byte[] GetChainId(ulong serialNumber)
        {
            var bytes = Encoding.UTF8.GetBytes(serialNumber + "_AElf").CalculateHash().Take(ChainIdLength).ToArray();
            return bytes;
        }
    }
}