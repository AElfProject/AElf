using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

namespace AElf.Kernel;

public class Bloom
{
    private const int Length = 256;

    private const uint BucketPerVal = 3; // number of hash functions

    public Bloom()
    {
        Data = new byte[0];
    }

    public Bloom(byte[] data)
    {
        if (data.Length == 0)
        {
            Data = new byte[0];
            return;
        }

        if (data.Length != Length) throw new InvalidOperationException($"Bloom data has to be {Length} bytes long.");

        Data = (byte[])data.Clone();
    }

    public Bloom(Bloom bloom) : this(bloom.Data)
    {
    }

    public byte[] Data { get; private set; }

    public static byte[] AndMultipleBloomBytes(IEnumerable<byte[]> multipleBytes)
    {
        var res = new byte[Length];
        foreach (var bytes in multipleBytes)
            for (var i = 0; i < Length; i++)
                res[i] |= bytes[i];
        return res;
    }

    public void AddValue(byte[] bytes)
    {
        AddSha256Hash(bytes.ComputeHash());
    }

    public void AddValue(IMessage message)
    {
        if (message == null) return;
        var bytes = message.ToByteArray();
        AddValue(bytes);
    }

    public void AddSha256Hash(byte[] hash256)
    {
        if (hash256.Length != 32) throw new InvalidOperationException("Invalid input.");

        if (Data.Length == 0) Data = new byte[Length];
        for (uint i = 0; i < BucketPerVal * 2; i += 2)
        {
            var index = ((hash256[i] << 8) | hash256[i + 1]) & 2047;
            var byteToSet = (byte)((uint)1 << (index % 8));
            Data[255 - index / 8] |= byteToSet;
        }
    }

    /// <summary>
    ///     Combines some other blooms into current bloom.
    /// </summary>
    /// <param name="blooms">Other blooms</param>
    public void Combine(IEnumerable<Bloom> blooms)
    {
        if (blooms == null) return;
        var bloomsList = blooms.ToList();
        if (!bloomsList.Any(b => b.Data.Length > 0)) return;
        if (Data.Length == 0) Data = new byte[Length];
        foreach (var bloom in bloomsList)
            for (var i = 0; i < Length; i++)
                Data[i] |= bloom.Data[i];
    }

    /// <summary>
    ///     Checks if current bloom is contained in the input bloom.
    /// </summary>
    /// <param name="bloom">Other bloom</param>
    /// <returns></returns>
    public bool IsIn(Bloom bloom)
    {
        if (bloom.Data.Length == 0 || Data.Length == 0) return false;
        for (var i = 0; i < Length; i++)
        {
            var curByte = Data[i];
            if ((curByte & bloom.Data[i]) != curByte) return false;
        }

        return true;
    }
}