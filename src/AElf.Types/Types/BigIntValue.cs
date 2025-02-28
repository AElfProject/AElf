using System;
using System.Linq;
using System.Numerics;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Types
{
    public partial class BigIntValue : IComparable, IComparable<BigIntValue>
    {
        #region Frequent Values

        public static BigIntValue Zero => new BigIntValue { Value = "0" };
        public static BigIntValue One => new BigIntValue { Value = "1" };

        #endregion

        public static BigIntValue FromBigEndianBytes(byte[] bigEndianBytes)
        {
            var bigInteger = new BigInteger(bigEndianBytes, true, true);
            return new BigIntValue
            {
                Value = bigInteger.ToString()
            };
        }

        public byte[] ToBigEndianBytes()
        {
            var bigInteger = ConvertStringToBigInteger(Value);
            return bigInteger.ToByteArray(true, true);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is BigIntValue bigInt)) throw new InvalidOperationException();

            return CompareTo(bigInt);
        }

        public int CompareTo(BigIntValue other)
        {
            if (LessThan(this, other)) return -1;

            if (Value == other.Value) return 0;

            return 1;
        }

        public static implicit operator BigIntValue(string str)
        {
            if (str.All(c => '0' <= c || c <= '9' || c == '_' || c == '-'))
            {
                if (str.Contains('-'))
                    if (!str.StartsWith("-") || str.Count(c => c == '-') > 1)
                        throw new ArgumentException("Invalid big integer string.");

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

        public static implicit operator BigIntValue(Int32Value value)
        {
            return new BigIntValue
            {
                Value = value.Value.ToString()
            };
        }

        public static implicit operator BigIntValue(Int64Value value)
        {
            return new BigIntValue
            {
                Value = value.Value.ToString()
            };
        }

        public static implicit operator BigInteger(BigIntValue value)
        {
            return ConvertStringToBigInteger(value.Value);
        }

        private static BigInteger ConvertStringToBigInteger(string str)
        {
            str = str.Replace("_", string.Empty);
            if (BigInteger.TryParse(str, out var bigInteger)) return bigInteger;

            throw new ArgumentException("Incorrect arguments.");
        }

        private static bool LessThan(in BigIntValue a, in BigIntValue b)
        {
            var aBigInt = ConvertStringToBigInteger(a.Value);
            var bBigInt = ConvertStringToBigInteger(b.Value);
            return aBigInt < bBigInt;
        }

        #region Operators

        public static BigIntValue operator %(BigIntValue a, BigIntValue b)
        {
            return BigInteger.Remainder(ConvertStringToBigInteger(a.Value), ConvertStringToBigInteger(b.Value))
                .ToString();
        }

        public static BigIntValue operator +(BigIntValue a, BigIntValue b)
        {
            return BigInteger.Add(ConvertStringToBigInteger(a.Value), ConvertStringToBigInteger(b.Value)).ToString();
        }

        public static BigIntValue operator -(BigIntValue a, BigIntValue b)
        {
            return BigInteger.Subtract(ConvertStringToBigInteger(a.Value), ConvertStringToBigInteger(b.Value))
                .ToString();
        }

        public static BigIntValue operator *(BigIntValue a, BigIntValue b)
        {
            return BigInteger.Multiply(ConvertStringToBigInteger(a.Value), ConvertStringToBigInteger(b.Value))
                .ToString();
        }

        public static bool operator ==(BigIntValue a, BigIntValue b)
        {
            return ConvertStringToBigInteger(a?.Value ?? "0") == ConvertStringToBigInteger(b?.Value ?? "0");
        }

        public static bool operator !=(BigIntValue a, BigIntValue b)
        {
            return !(a == b);
        }

        #endregion

        #region < <= > >=

        public static bool operator <(in BigIntValue a, in BigIntValue b)
        {
            return LessThan(in a, in b);
        }

        public static bool operator >(in BigIntValue a, in BigIntValue b)
        {
            return LessThan(in b, in a);
        }

        public static bool operator >=(in BigIntValue a, in BigIntValue b)
        {
            return !LessThan(in a, in b);
        }

        public static bool operator <=(in BigIntValue a, in BigIntValue b)
        {
            return !LessThan(in b, in a);
        }

        #endregion
    }
}