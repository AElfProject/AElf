using System.Numerics;

namespace Scale.Encoders;

/// <summary>
/// Compact integer encoding.
/// </summary>
public class CompactIntegerTypeEncoder
{
    public byte[] Encode(ulong value)
    {
        switch (value)
        {
            case <= 63:
                return [(byte)(value << 2)];

            case <= 0x3FFF:
                return
                [
                    (byte)(((value & 0x3f) << 2) | 0x01),
                    (byte)((value & 0xFFC0) >> 6)
                ];

            case <= 0x3FFFFFFF:
                return
                [
                    (byte)(((value & 0x3F) << 2) | 0x02),
                    (byte)(value >> 6),
                    (byte)(value >> 14),
                    (byte)(value >> 22)
                ];

            default:
            {
                var bytes = new List<byte>();
                while (value > 0)
                {
                    bytes.Add((byte)(value & 0xFF));
                    value >>= 8;
                }

                var result = new List<byte>
                {
                    (byte)(((bytes.Count - 4) << 2) | 0x03)
                };
                result.AddRange(bytes);
                return result.ToArray();
            }
        }
    }

    public byte[] Encode(BigInteger value)
    {
        if (value <= 0x3F)
        {
            return [(byte)(value << 2)];
        }

        if (value <= 0x3FFF)
        {
            return
            [
                (byte)((value << 2) | 0x01),
                (byte)(value >> 6)
            ];
        }

        if (value <= 0x3FFFFFFF)
        {
            return
            [
                (byte)((value << 2) | 0x02),
                (byte)(value >> 6),
                (byte)(value >> 14),
                (byte)(value >> 22)
            ];
        }

        var bytes = value.ToByteArray();
        var result = new byte[1 + bytes.Length];
        result[0] = 0x0b;
        Array.Copy(bytes, 0, result, 1, bytes.Length);
        return result;
    }
}