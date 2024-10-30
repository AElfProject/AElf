using System.Numerics;

namespace Scale;

public struct CompactIntegerType
{
    public BigInteger Value { get; }

    public CompactIntegerType(BigInteger value)
    {
        Value = value;
    }

    public static CompactIntegerType Decode(byte[] value)
    {
        var p = 0;
        return Decode(value, ref p);
    }

    public static CompactIntegerType Decode(byte[] value, ref int p)
    {
        uint firstByte = value[p++];
        var flag = firstByte & 0b00000011u;
        CompactIntegerType number = 0u;

        switch (flag)
        {
            case 0b00u:
            {
                number = firstByte >> 2;
                break;
            }

            case 0b01u:
            {
                uint secondByte = value[p++];

                number = ((firstByte & 0b11111100u) + secondByte * 256u) >> 2;
                break;
            }

            case 0b10u:
            {
                number = firstByte;
                var multiplier = 256u;

                for (var i = 0; i < 3; ++i)
                {
                    number += value[p++] * multiplier;
                    multiplier <<= 8;
                }

                number >>= 2;
                break;
            }

            case 0b11:
            {
                var bytesCount = (firstByte >> 2) + 4u;
                CompactIntegerType multiplier = 1u;
                CompactIntegerType bigValue = 0;

                // we assured that there are m more bytes,
                // no need to make checks in a loop
                for (var i = 0; i < bytesCount; ++i)
                {
                    bigValue += multiplier * value[p++];
                    multiplier *= 256u;
                }

                return bigValue;
            }
        }

        return number;
    }

    public byte[] Encode()
    {
        if (this <= 63) return [this << 2];

        if (this <= 0x3FFF)
            return
            [
                ((this & 0x3F) << 2) | 0x01,
                (this & 0xFFC0) >> 6
            ];

        if (this <= 0x3FFFFFFF)
        {
            var result = new byte[4];
            result[0] = ((this & 0x3F) << 2) | 0x02;
            this >>= 6;
            for (var i = 1; i < 4; ++i)
            {
                result[i] = this & 0xFF;
                this >>= 8;
            }

            return result;
        }
        else
        {
            var b0 = new List<byte>();
            while (this > 0)
            {
                b0.Add(this & 0xFF);
                this >>= 8;
            }

            var result = new List<byte>
            {
                (byte)(((b0.Count - 4) << 2) | 0x03)
            };
            result.AddRange(b0);
            return result.ToArray();
        }
    }

    public static CompactIntegerType operator /(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value / value.Value;
    }

    public static CompactIntegerType operator -(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value - value.Value;
    }

    public static CompactIntegerType operator +(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value + value.Value;
    }

    public static CompactIntegerType operator *(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value * value.Value;
    }

    public static bool operator <(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value < value.Value;
    }

    public static bool operator >(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value > value.Value;
    }

    public static bool operator <=(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value <= value.Value;
    }

    public static bool operator >=(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value >= value.Value;
    }

    public static CompactIntegerType operator <<(CompactIntegerType self, int value)
    {
        return self.Value << value;
    }

    public static CompactIntegerType operator >> (CompactIntegerType self, int value)
    {
        return self.Value >> value;
    }

    public static CompactIntegerType operator &(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value & value.Value;
    }

    public static CompactIntegerType operator |(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value | value.Value;
    }

    public static bool operator ==(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value == value.Value;
    }

    public static bool operator !=(CompactIntegerType self, CompactIntegerType value)
    {
        return self.Value != value.Value;
    }

    public static explicit operator BigInteger(CompactIntegerType c)
    {
        return c.Value;
    }

    public static implicit operator sbyte(CompactIntegerType c)
    {
        return (sbyte)c.Value;
    }

    public static implicit operator byte(CompactIntegerType c)
    {
        return (byte)c.Value;
    }

    public static implicit operator short(CompactIntegerType c)
    {
        return (short)c.Value;
    }

    public static implicit operator ushort(CompactIntegerType c)
    {
        return (ushort)c.Value;
    }

    public static implicit operator int(CompactIntegerType c)
    {
        return (int)c.Value;
    }

    public static implicit operator uint(CompactIntegerType c)
    {
        return (uint)c.Value;
    }

    public static implicit operator long(CompactIntegerType c)
    {
        return (long)c.Value;
    }

    public static implicit operator ulong(CompactIntegerType c)
    {
        return (ulong)c.Value;
    }

    public static implicit operator CompactIntegerType(BigInteger b)
    {
        return new CompactIntegerType(b);
    }

    public static implicit operator CompactIntegerType(sbyte i)
    {
        return new CompactIntegerType(i);
    }

    public static implicit operator CompactIntegerType(byte i)
    {
        return new CompactIntegerType(i);
    }

    public static implicit operator CompactIntegerType(short i)
    {
        return new CompactIntegerType(i);
    }

    public static implicit operator CompactIntegerType(ushort i)
    {
        return new CompactIntegerType(i);
    }

    public static implicit operator CompactIntegerType(int i)
    {
        return new CompactIntegerType(i);
    }

    public static implicit operator CompactIntegerType(uint i)
    {
        return new CompactIntegerType(i);
    }

    public static implicit operator CompactIntegerType(long i)
    {
        return new CompactIntegerType(i);
    }

    public static implicit operator CompactIntegerType(ulong i)
    {
        return new CompactIntegerType(i);
    }

    public override bool Equals(object obj)
    {
        if (obj is CompactIntegerType i) return Value.Equals(i.Value);
        return Value.Equals(obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}