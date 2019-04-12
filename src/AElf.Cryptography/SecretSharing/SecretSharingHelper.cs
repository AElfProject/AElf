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
        public static List<string> EncodeSecret(string secretMessage, int threshold, int totalParts)
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
                    secretBigInteger %= SecretSharingConsts.FieldPrime;
                }

                result.Add(Convert.ToBase64String(secretBigInteger.ToByteArray()));
            }

            return result;
        }

        // The shared parts must be sent in order.
        public static string DecodeSecret(List<string> sharedParts, List<int> orders, int threshold)
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

                    (numerator, denominator) =
                        MultiplyRational(numerator, denominator, orders[j], orders[j] - orders[i]);
                }

                result += RationalToWhole(numerator, denominator);
                result %= SecretSharingConsts.FieldPrime;
            }

            return result.ConvertToString();
        }

        private static BigInteger RationalToWhole(BigInteger numerator, BigInteger denominator)
        {
            return numerator * Inverse(denominator) % SecretSharingConsts.FieldPrime;
        }

        private static BigInteger GetGreatestCommonDivisor(BigInteger integer1, BigInteger integer2)
        {
            while (true)
            {
                if (integer2 == 0) return integer1;
                var integer3 = integer1;
                integer1 = integer2;
                integer2 = integer3 % integer2;
            }
        }

        private static (BigInteger gcd, BigInteger invA, BigInteger invB) GetGreatestCommonDivisor2(BigInteger integer1, BigInteger integer2)
        {
            if (integer2 == 0)
            {
                return (integer1, 1, 0);
            }
            
            var div = BigInteger.DivRem(integer1, integer2, out var rem);
            var (g, iA, iB) = GetGreatestCommonDivisor2(integer2, rem);
            return (g, iB, iA - iB * div);
        }

        private static BigInteger Inverse(BigInteger integer)
        {
            return GetGreatestCommonDivisor2(SecretSharingConsts.FieldPrime, integer).invB.Abs();
        }
        
        private static (BigInteger numerator, BigInteger denominator) MultiplyRational(
            BigInteger numeratorLhs, BigInteger denominatorLhs,
            BigInteger numeratorRhs, BigInteger denominatorRhs)
        {
            denominatorRhs = denominatorRhs.Abs();
            var numerator = numeratorLhs * numeratorRhs % SecretSharingConsts.FieldPrime;
            var denominator = denominatorLhs * denominatorRhs % SecretSharingConsts.FieldPrime;
            var gcd = GetGreatestCommonDivisor(numerator, denominator);
            return (numerator / gcd, denominator / gcd);
        }
    }
}