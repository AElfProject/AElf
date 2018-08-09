using System;
using System.Collections.Generic;
using Google.Protobuf;
using System.Security.Cryptography;

namespace AElf.Kernel
{
    public class Bloom
    {
        private const uint BucketPerVal = 3; // number of hash functions
        private byte[] _data = new byte[256];

        public byte[] Data => _data;

        public Bloom(){}
        
        public Bloom(Bloom bloom)
        {
            if (bloom.Data.Length != 256)
            {
                throw new InvalidOperationException("Bloom data has to be 256 bytes long.");
            }
            _data = (byte[]) bloom.Data.Clone();
        }
        
        public void AddValue(byte[] bytes)
        {
            var hash = SHA256.Create().ComputeHash(bytes);
            AddSha256Hash(hash);
        }
        
        public void AddValue(IMessage message)
        {
            var bytes = message.ToByteArray();
            AddValue(bytes);
        }

        public void AddValue(ISerializable serializable)
        {
            var bytes = serializable.Serialize();
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
                var byteToSet = (byte)(((uint)1) << (index % 8));
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
                for (int i = 0; i < 256; i++)
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
            for (int i = 0; i < 256; i++)
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