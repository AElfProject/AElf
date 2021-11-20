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

        #region UInt256 / Int256

        public static UInt256Value Mul(this UInt256Value a, UInt256Value b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return new UInt256Value
            {
                Value = (aBigInt * bBigInt).ToString()
            };
        }

        public static UInt256Value Div(this UInt256Value a, UInt256Value b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return new UInt256Value
            {
                Value = (aBigInt / bBigInt).ToString()
            };
        }

        public static UInt256Value Sub(this UInt256Value a, UInt256Value b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return new UInt256Value
            {
                Value = (aBigInt - bBigInt).ToString()
            };
        }

        public static UInt256Value Add(this UInt256Value a, UInt256Value b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return new UInt256Value
            {
                Value = (aBigInt + bBigInt).ToString()
            };
        }
        
        
        public static Int256Value Mul(this Int256Value a, Int256Value b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return new Int256Value
            {
                Value = (aBigInt * bBigInt).ToString()
            };
        }

        public static Int256Value Div(this Int256Value a, Int256Value b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return new Int256Value
            {
                Value = (aBigInt / bBigInt).ToString()
            };
        }

        public static Int256Value Sub(this Int256Value a, Int256Value b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return new Int256Value
            {
                Value = (aBigInt - bBigInt).ToString()
            };
        }

        public static Int256Value Add(this Int256Value a, Int256Value b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return new Int256Value
            {
                Value = (aBigInt + bBigInt).ToString()
            };
        }

        private static BigInteger ConvertStringToBigInteger(string str)
        {
            str = str.Replace("_", string.Empty);
            if (BigInteger.TryParse(str, out var bigInteger))
            {
                return bigInteger;
            }

            throw new ArgumentException("Incorrect arguments.");
        }

        #endregion
    }
}