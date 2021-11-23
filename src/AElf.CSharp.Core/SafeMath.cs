using System;
using System.Numerics;
using AElf.Types;

namespace AElf.CSharp.Core
{
    /// <summary>
    /// Helper methods for safe math operations that explicitly check for overflow.
    /// </summary>
    public static class SafeMath
    {
        #region int

        public static int Mul(this int a, int b)
        {
            checked
            {
                return a * b;
            }
        }

        public static int Div(this int a, int b)
        {
            checked
            {
                return a / b;
            }
        }

        public static int Sub(this int a, int b)
        {
            checked
            {
                return a - b;
            }
        }

        public static int Add(this int a, int b)
        {
            checked
            {
                return a + b;
            }
        }

        #endregion int

        #region uint

        public static uint Mul(this uint a, uint b)
        {
            checked
            {
                return a * b;
            }
        }

        public static uint Div(this uint a, uint b)
        {
            checked
            {
                return a / b;
            }
        }

        public static uint Sub(this uint a, uint b)
        {
            checked
            {
                return a - b;
            }
        }

        public static uint Add(this uint a, uint b)
        {
            checked
            {
                return a + b;
            }
        }

        #endregion uint

        #region long

        public static long Mul(this long a, long b)
        {
            checked
            {
                return a * b;
            }
        }

        public static long Div(this long a, long b)
        {
            checked
            {
                return a / b;
            }
        }

        public static long Sub(this long a, long b)
        {
            checked
            {
                return a - b;
            }
        }

        public static long Add(this long a, long b)
        {
            checked
            {
                return a + b;
            }
        }

        #endregion long

        #region ulong

        public static ulong Mul(this ulong a, ulong b)
        {
            checked
            {
                return a * b;
            }
        }

        public static ulong Div(this ulong a, ulong b)
        {
            checked
            {
                return a / b;
            }
        }

        public static ulong Sub(this ulong a, ulong b)
        {
            checked
            {
                return a - b;
            }
        }

        public static ulong Add(this ulong a, ulong b)
        {
            checked
            {
                return a + b;
            }
        }

        #endregion ulong

        #region BigIntValue

        public static BigIntValue Mul(this BigIntValue a, BigIntValue b)
        {
            return new BigIntValue
            {
                Value = BigInteger.Multiply(a, b).ToString()
            };
        }

        public static BigIntValue Div(this BigIntValue a, BigIntValue b)
        {
            return new BigIntValue
            {
                Value = BigInteger.Divide(a, b).ToString()
            };
        }

        public static BigIntValue Sub(this BigIntValue a, BigIntValue b)
        {
            return new BigIntValue
            {
                Value = BigInteger.Subtract(a, b).ToString()
            };
        }

        public static BigIntValue Add(this BigIntValue a, BigIntValue b)
        {
            return new BigIntValue
            {
                Value = BigInteger.Add(a, b).ToString()
            };
        }

        public static BigIntValue Pow(this BigIntValue a, int exponent)
        {
            BigInteger bigInt = a;
            return new BigIntValue
            {
                Value = BigInteger.Pow(bigInt, exponent).ToString()
            };
        }

        public static BigIntValue Or(this BigIntValue a, BigIntValue b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new BigIntValue
            {
                Value = (aBigInt | bBigInt).ToString()
            };
        }

        public static BigIntValue Xor(this BigIntValue a, BigIntValue b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new BigIntValue
            {
                Value = (aBigInt ^ bBigInt).ToString()
            };
        }

        public static BigIntValue And(this BigIntValue a, BigIntValue b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new BigIntValue
            {
                Value = (aBigInt & bBigInt).ToString()
            };
        }

        public static BigIntValue Not(this BigIntValue a)
        {
            BigInteger bigInt = a;
            return new BigIntValue
            {
                Value = (~bigInt).ToString()
            };
        }

        public static BigIntValue Abs(this BigIntValue a)
        {
            return new BigIntValue
            {
                Value = BigInteger.Abs(a).ToString()
            };
        }

        public static BigIntValue Negate(this BigIntValue a)
        {
            return new BigIntValue
            {
                Value = BigInteger.Negate(a).ToString()
            };
        }

        public static BigIntValue Remainder(this BigIntValue dividend, BigIntValue divisor)
        {
            return new BigIntValue
            {
                Value = BigInteger.Remainder(dividend, divisor).ToString()
            };
        }

        /// <summary>
        /// Divides one BigIntValue value by another, returns the result, and returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend"></param>
        /// <param name="divisor"></param>
        /// <param name="remainder"></param>
        /// <returns></returns>
        public static BigIntValue DivRem(this BigIntValue dividend, BigIntValue divisor, out BigIntValue remainder)
        {
            var result = BigInteger.DivRem(dividend, divisor, out var originRemainder).ToString();
            remainder = new BigIntValue
            {
                Value = originRemainder.ToString()
            };
            return result;
        }

        /// <summary>
        /// Performs modulus division on a number raised to the power of another number.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="exponent"></param>
        /// <param name="modulus"></param>
        /// <returns></returns>
        public static BigIntValue ModPow(this BigIntValue value, BigIntValue exponent, BigIntValue modulus)
        {
            return new BigIntValue
            {
                Value = BigInteger.ModPow(value, exponent, modulus).ToString()
            };
        }

        #endregion
    }
}