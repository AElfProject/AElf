using System.Numerics;

namespace AElf.Cryptography.SecretSharing
{
    public static class SecretSharingConsts
    {
        public static readonly uint MaxBits = 1024;
        public static readonly BigInteger FieldPrime = BigInteger.Pow(new BigInteger(2), 1025) - 1;
    }
}