using System;
using System.Numerics;

namespace AElf.Common
{
    public static class ChainHelpers
    {
        public static byte[] GetRandomChainId()
        {
            Random r = new Random();
            BigInteger randomBigInt = r.Next(198535, 11316496);
            var bigIntByteArray = randomBigInt.ToByteArray();
            
            var randomBytes = new byte[3];
            randomBytes[0] = bigIntByteArray[2];
            randomBytes[1] = bigIntByteArray[1];
            randomBytes[2] = bigIntByteArray[0];

            return randomBytes;
        }
    }
}