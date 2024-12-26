using Nethereum.Util;

namespace AElf.Cryptography.Keccak
{
    public static class KeccakHelper
    {
        public static byte[] Keccak256(byte[] message)
        {
            return Sha3Keccack.Current.CalculateHash(message);
        }
    }
}