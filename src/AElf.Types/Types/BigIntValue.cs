using System;
using System.Linq;
using System.Numerics;

namespace AElf.Types
{
    public partial class BigIntValue
    {
        public static implicit operator BigIntValue(string str)
        {
            if (str.All(c => '0' <= c || c <= '9' || c == '_' || c == '-'))
            {
                if (str.Contains('-'))
                {
                    if (!str.StartsWith("-") || str.Count(c => c == '-') > 1)
                    {
                        throw new ArgumentException("Invalid big integer string.");
                    }
                }

                str = str.Replace("_", string.Empty);
                return new BigIntValue
                {
                    Value = str
                };
            }

            throw new ArgumentException("Invalid big integer string.");
        }

        public static implicit operator BigIntValue(ushort value)
        {
            return new BigIntValue
            {
                Value = value.ToString()
            };
        }

        public static implicit operator BigIntValue(short value)
        {
            return new BigIntValue
            {
                Value = value.ToString()
            };
        }

        public static implicit operator BigIntValue(uint value)
        {
            return new BigIntValue
            {
                Value = value.ToString()
            };
        }

        public static implicit operator BigIntValue(int value)
        {
            return new BigIntValue
            {
                Value = value.ToString()
            };
        }

        public static implicit operator BigIntValue(ulong value)
        {
            return new BigIntValue
            {
                Value = value.ToString()
            };
        }

        public static implicit operator BigIntValue(long value)
        {
            return new BigIntValue
            {
                Value = value.ToString()
            };
        }

        public static implicit operator BigInteger(BigIntValue value)
        {
            return ConvertStringToBigInteger(value.Value);
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
    }
}