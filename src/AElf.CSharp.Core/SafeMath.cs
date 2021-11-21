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

        #region UInt256 / Int256 for commen style

        public static UInt256Value Mul(this UInt256Value a, UInt256Value b)
        {
            return new UInt256Value
            {
                Value = BigInteger.Multiply(a, b).ToString()
            };
        }

        public static UInt256Value Div(this UInt256Value a, UInt256Value b)
        {
            return new UInt256Value
            {
                Value = BigInteger.Divide(a, b).ToString()
            };
        }

        public static UInt256Value Sub(this UInt256Value a, UInt256Value b)
        {
            return new UInt256Value
            {
                Value = BigInteger.Subtract(a, b).ToString()
            };
        }

        public static UInt256Value Add(this UInt256Value a, UInt256Value b)
        {
            return new UInt256Value
            {
                Value = BigInteger.Add(a, b).ToString()
            };
        }

        public static UInt256Value Pow(this UInt256Value a, int exponent)
        {
            BigInteger bigInt = a;
            return new UInt256Value
            {
                Value = BigInteger.Pow(bigInt, exponent).ToString()
            };
        }

        public static UInt256Value Or(this UInt256Value a, UInt256Value b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new UInt256Value
            {
                Value = (aBigInt | bBigInt).ToString()
            };
        }

        public static UInt256Value Xor(this UInt256Value a, UInt256Value b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new UInt256Value
            {
                Value = (aBigInt ^ bBigInt).ToString()
            };
        }

        public static UInt256Value And(this UInt256Value a, UInt256Value b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new UInt256Value
            {
                Value = (aBigInt & bBigInt).ToString()
            };
        }

        public static UInt256Value Not(this UInt256Value a)
        {
            BigInteger bigInt = a;
            return new UInt256Value
            {
                Value = (~bigInt).ToString()
            };
        }

        public static UInt256Value Abs(this UInt256Value a)
        {
            return new UInt256Value
            {
                Value = BigInteger.Abs(a).ToString()
            };
        }

        public static UInt256Value Negate(this UInt256Value a)
        {
            return new UInt256Value
            {
                Value = BigInteger.Negate(a).ToString()
            };
        }

        public static UInt256Value Remainder(this UInt256Value dividend, UInt256Value divisor)
        {
            return new UInt256Value
            {
                Value = BigInteger.Remainder(dividend, divisor).ToString()
            };
        }

        /// <summary>
        /// Divides one UInt256Value value by another, returns the result, and returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend"></param>
        /// <param name="divisor"></param>
        /// <param name="remainder"></param>
        /// <returns></returns>
        public static UInt256Value DivRem(this UInt256Value dividend, UInt256Value divisor, out UInt256Value remainder)
        {
            var result = BigInteger.DivRem(dividend, divisor, out var originRemainder).ToString();
            remainder = new UInt256Value
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
        public static UInt256Value ModPow(this UInt256Value value, UInt256Value exponent, UInt256Value modulus)
        {
            return new UInt256Value
            {
                Value = BigInteger.ModPow(value, exponent, modulus).ToString()
            };
        }

        public static Int256Value Mul(this Int256Value a, Int256Value b)
        {
            return new Int256Value
            {
                Value = BigInteger.Multiply(a, b).ToString()
            };
        }

        public static Int256Value Div(this Int256Value a, Int256Value b)
        {
            return new Int256Value
            {
                Value = BigInteger.Divide(a, b).ToString()
            };
        }

        public static Int256Value Sub(this Int256Value a, Int256Value b)
        {
            return new Int256Value
            {
                Value = BigInteger.Subtract(a, b).ToString()
            };
        }

        public static Int256Value Add(this Int256Value a, Int256Value b)
        {
            return new Int256Value
            {
                Value = BigInteger.Add(a, b).ToString()
            };
        }

        public static Int256Value Pow(this Int256Value a, int exponent)
        {
            BigInteger bigInt = a;
            return new Int256Value
            {
                Value = BigInteger.Pow(bigInt, exponent).ToString()
            };
        }

        public static Int256Value Or(this Int256Value a, Int256Value b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new Int256Value
            {
                Value = (aBigInt | bBigInt).ToString()
            };
        }

        public static Int256Value Xor(this Int256Value a, Int256Value b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new Int256Value
            {
                Value = (aBigInt ^ bBigInt).ToString()
            };
        }

        public static Int256Value And(this Int256Value a, Int256Value b)
        {
            BigInteger aBigInt = a;
            BigInteger bBigInt = b;
            return new Int256Value
            {
                Value = (aBigInt & bBigInt).ToString()
            };
        }

        public static Int256Value Not(this Int256Value a)
        {
            BigInteger bigInt = a;
            return new Int256Value
            {
                Value = (~bigInt).ToString()
            };
        }

        public static Int256Value Abs(this Int256Value a)
        {
            return new Int256Value
            {
                Value = BigInteger.Abs(a).ToString()
            };
        }

        public static Int256Value Negate(this Int256Value a)
        {
            return new Int256Value
            {
                Value = BigInteger.Negate(a).ToString()
            };
        }

        public static Int256Value Remainder(this Int256Value dividend, Int256Value divisor)
        {
            return new Int256Value
            {
                Value = BigInteger.Remainder(dividend, divisor).ToString()
            };
        }

        /// <summary>
        /// Divides one Int256Value value by another, returns the result, and returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend"></param>
        /// <param name="divisor"></param>
        /// <param name="remainder"></param>
        /// <returns></returns>
        public static Int256Value DivRem(this Int256Value dividend, Int256Value divisor, out Int256Value remainder)
        {
            var result = BigInteger.DivRem(dividend, divisor, out var originRemainder).ToString();
            remainder = new Int256Value
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
        public static Int256Value ModPow(this Int256Value value, Int256Value exponent, Int256Value modulus)
        {
            return new Int256Value
            {
                Value = BigInteger.ModPow(value, exponent, modulus).ToString()
            };
        }

        #endregion
    }
}