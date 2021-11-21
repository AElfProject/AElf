using System;
using System.Linq;
using System.Numerics;

namespace AElf.Types
{
    public partial class UInt256Value
    {
        public static implicit operator UInt256Value(string str)
        {
            if (str.All(c => '0' <= c || c <= '9' || c == '_'))
            {
                str = str.Replace("_", string.Empty);
                return new UInt256Value
                {
                    Value = str
                };
            }

            throw new ArgumentException("Invalid big integer string.");
        }

        public static implicit operator UInt256Value(ushort value)
        {
            return new UInt256Value
            {
                Value = value.ToString()
            };
        }

        public static implicit operator UInt256Value(short value)
        {
            return new UInt256Value
            {
                Value = value.ToString()
            };
        }

        public static implicit operator UInt256Value(uint value)
        {
            return new UInt256Value
            {
                Value = value.ToString()
            };
        }

        public static implicit operator UInt256Value(int value)
        {
            return new UInt256Value
            {
                Value = value.ToString()
            };
        }

        public static implicit operator UInt256Value(ulong value)
        {
            return new UInt256Value
            {
                Value = value.ToString()
            };
        }

        public static implicit operator UInt256Value(long value)
        {
            return new UInt256Value
            {
                Value = value.ToString()
            };
        }

        public static implicit operator BigInteger(UInt256Value value)
        {
            return ConvertStringToBigInteger(value.Value);
        }
        
        public static implicit operator Int256Value(UInt256Value value)
        {
            return new Int256Value
            {
                Value = value.Value
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
    }
}