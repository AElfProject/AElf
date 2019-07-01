using System;

namespace AElf
{
    public static class ChainHelper
    {
        public static int GetChainId(long serialNumber)
        {
            // For 4 base58 chars use following range (2111 ~ zzzz):
            // Max: 57*58*58*58+57*58*58+57*58+57 = 11316496 (zzzz)
            // Min: 1*58*58*58+0*58*58+0*58+0 = 195112 (2111)
            var validNUmber = (uint) serialNumber.GetHashCode() % 11316496;
            if (validNUmber < 195112)
                validNUmber += 195112;

            var validNUmberBytes = BitConverter.GetBytes(validNUmber);

            // Use BigInteger(BigEndian) format (bytes size = 3)
            var integerBytes = new byte[4];
            for (var i = 0; i < 3; i++)
                integerBytes[2 - i] = validNUmberBytes[i];

            return BitConverter.ToInt32(integerBytes, 0);
        }

        public static string ConvertChainIdToBase58(int chainId)
        {
            // Default chain id is 4 base58 chars, bytes size is 3
            var bytes = BitConverter.GetBytes(chainId);
            Array.Resize(ref bytes, 3);
            return bytes.ToPlainBase58();
        }

        public static int ConvertBase58ToChainId(string base58String)
        {
            // Use int type to save chain id (4 base58 chars, default is 3 bytes)
            var bytes = base58String.DecodeBase58();
            if (bytes.Length < 4)
                Array.Resize(ref bytes, 4);

            return BitConverter.ToInt32(bytes, 0);
        }
    }
}