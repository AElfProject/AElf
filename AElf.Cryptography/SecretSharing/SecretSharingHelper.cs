using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AElf.Common;

namespace AElf.Cryptography.SecretSharing
{
    /// <summary>
    /// Implementation of Shamir's Secret Sharing: https://en.wikipedia.org/wiki/Shamir%27s_Secret_Sharing
    /// </summary>
    public static class SecretSharingHelper
    {
        private static readonly BigInteger PrimeNumber = BigInteger.Pow(new BigInteger(2), 521) - 1;
        
        public static List<string> SplitSecret(string secretMessage, int threshold, int totalParts)
        {
            // Polynomial construction.
            var coefficients = new BigInteger[threshold];
            // Set p(0) = secret message.
            coefficients[0] = secretMessage.ToBigInteger();
            for (var i = 1; i < threshold; i++)
            {
                var foo = new byte[32];
                Array.Copy(Hash.Generate().ToArray(), foo, 32);
                coefficients[i] = BigInteger.Abs(new BigInteger(foo));
            }

            var result = new List<string>();
            for (var i = 1; i < totalParts + 1; i++)
            {
                var secretBigInteger = coefficients[0];
                for (var j = 1; j < threshold; j++)
                {
                    secretBigInteger += coefficients[j] * BigInteger.Pow(new BigInteger(i), j);
                    secretBigInteger %= PrimeNumber;
                }

                result.Add(Convert.ToBase64String(secretBigInteger.ToByteArray()));
            }

            return result;
        }

        // The shared parts must be sent in order.
        public static string MergeSecret(List<string> sharedParts, int threshold)
        {
            var result = BigInteger.Zero;
            
            for (var i = 0; i < threshold; i++)
            {
                var numerator = new BigInteger(Convert.FromBase64String(sharedParts[i]));
                var denominator = BigInteger.One;
                for (var j = 0; j < threshold; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var numeratorRhs = j + 1;
                    var denominatorRhs = Math.Abs(j - i);
                    numerator = (numerator * numeratorRhs) % PrimeNumber;
                    denominator = (denominator * denominatorRhs) % PrimeNumber;
                    var gcd = GCD(numerator, denominator);
                    numerator /= gcd;
                    denominator /= gcd;
                }

                result += RationalToWhole(numerator, denominator);
                result %= PrimeNumber;
            }

            return result.Decode();
        }

        private static BigInteger RationalToWhole(BigInteger numerator, BigInteger denominator)
        {
            return numerator * Inverse(denominator) % PrimeNumber;
        }

        private static BigInteger GCD(BigInteger integer1, BigInteger integer2)
        {
            while (true)
            {
                if (integer2 == 0) return integer1;
                var integer3 = integer1;
                integer1 = integer2;
                integer2 = integer3 % integer2;
            }
        }

        private static (BigInteger gcd, BigInteger integerA, BigInteger integerB) GCD2(BigInteger integer1, BigInteger integer2)
        {
            if (integer2 == 0)
                return (integer1, 1, 0);
            var div = BigInteger.DivRem(integer1, integer2, out var rem);
            var (gcd, integerA, integerB) = GCD2(integer2, rem);
            return (gcd, integerB, integerA - integerB * div);
        }

        private static BigInteger Inverse(BigInteger integer)
        {
            return BigInteger.Abs(GCD2(PrimeNumber, integer).integerB);
        }
    }
}