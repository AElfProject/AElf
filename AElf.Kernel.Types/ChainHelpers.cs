using System;
using System.Numerics;

namespace AElf.Common
{
    public static class ChainHelpers
    {
        public static int GetRandomChainId()
        {
            Random r = new Random();
            return r.Next(198535, 11316496);
        }

        public static int GetChainId(ulong serialNumber)
        {
            // For 4 base58 chars use following range (2111 ~ zzzz):
            // Max: 57*58*58*58+57*58*58+57*58+57 = 11316496 (zzzz)
            // Min: 1*58*58*58+0*58*58+0*58+0 = 195112 (2111)
            var invalidNUmber = serialNumber.GetHashCode() % 11316496;
            if (invalidNUmber < 195112)
                invalidNUmber += 195112;

            var invalidNUmberBytes = BitConverter.GetBytes(invalidNUmber);

            // Use BigInteger(BigEndian) format (bytes size = 3)
            var integerBytes = new byte[4];
            for (var i = 0; i < 3; i++)
                integerBytes[2 - i] = invalidNUmberBytes[i];

            return BitConverter.ToInt32(integerBytes, 0);
        }

        // TODO: helper method maybe better
        public static string DumpBase58(this int n)
        {
            // Default chain id is 4 base58 chars, bytes size is 3
            var bytes = BitConverter.GetBytes(n);
            Array.Resize(ref bytes, 3);
            return bytes.ToPlainBase58();
        }

        // TODO: helper method maybe better
        public static int ConvertBase58ToChainId(this string base58String)
        {
            // Use int type to save chain id (4 base58 chars, default is 3 bytes)
            var bytes = base58String.DecodeBase58();
            if (bytes.Length < 4)
                Array.Resize(ref bytes, 4);

            return BitConverter.ToInt32(bytes, 0);
        }
    }
}