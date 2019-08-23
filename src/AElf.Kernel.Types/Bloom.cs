using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class Bloom
    {
        private const int Length = 256;
        public static byte[] AndMultipleBloomBytes(IEnumerable<byte[]> multipleBytes)
        {
            var res = new byte[Length];
            foreach (var bytes in multipleBytes)
            {
                for (var i = 0; i < Length; i++)
                {
                    res[i] |= bytes[i];
                }
            }
            return res;
        }

        private const uint BucketPerVal = 3; // number of hash functions
        private byte[] _data;

        public byte[] Data => _data;

        public Bloom()
        {
            _data = new byte[Length];
        }

        public Bloom(byte[] data)
        {
            if (data.Length != Length)
            {
                throw new InvalidOperationException($"Bloom data has to be {Length} bytes long.");
            }

            _data = (byte[]) data.Clone();
        }

        public Bloom(Bloom bloom) : this(bloom.Data)
        {
        }

        public void AddValue(byte[] bytes)
        {
            AddSha256Hash(bytes.ComputeHash());
        }

        public void AddValue(IMessage message)
        {
            var bytes = message.ToByteArray();
            AddValue(bytes);
        }

        public void AddSha256Hash(byte[] hash256)
        {
            if (hash256.Length != 32)
            {
                throw new InvalidOperationException("Invalid input.");
            }

            for (uint i = 0; i < BucketPerVal * 2; i += 2)
            {
                var index = ((hash256[i] << 8) | hash256[i + 1]) & 2047;
                var byteToSet = (byte) (((uint) 1) << (index % 8));
                _data[255 - index / 8] |= byteToSet;
            }
        }

        /// <summary>
        /// Combines some other blooms into current bloom.
        /// </summary>
        /// <param name="blooms">Other blooms</param>
        public void Combine(IEnumerable<Bloom> blooms)
        {
            foreach (var bloom in blooms)
            {
                for (var i = 0; i < Length; i++)
                {
                    _data[i] |= bloom.Data[i];
                }
            }
        }

        /// <summary>
        /// Checks if current bloom is contained in the input bloom.
        /// </summary>
        /// <param name="bloom">Other bloom</param>
        /// <returns></returns>
        public bool IsIn(Bloom bloom)
        {
            for (var i = 0; i < Length; i++)
            {
                var curByte = _data[i];
                if ((curByte & bloom.Data[i]) != curByte)
                {
                    return false;
                }
            }

            return true;
        }
    }
}