using System.Text;
using AElf.Types;
using Google.Protobuf;

namespace AElf
{

    public class HashHelper
    {
        /// <summary>
        ///     Computes hash from a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(byte[] bytes)
        {
            return Hash.LoadFromByteArray(bytes.ComputeHash());
        }

        /// <summary>
        ///     Computes hash from a string encoded in UTF8.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(string str)
        {
            return ComputeFrom(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        ///     Computes hash from int32 value.
        /// </summary>
        /// <param name="intValue"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(int intValue)
        {
            return ComputeFrom(intValue.ToBytes(false));
        }

        /// <summary>
        ///     Computes hash from int64 value.
        /// </summary>
        /// <param name="intValue"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(long intValue)
        {
            return ComputeFrom(intValue.ToBytes(false));
        }

        /// <summary>
        ///     Gets the hash from a Protobuf Message. Its serialized byte array is used for hash calculation.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Hash ComputeFrom(IMessage message)
        {
            return ComputeFrom(message.ToByteArray());
        }

        /// <summary>
        ///     Computes a new hash from two input hashes with bitwise xor operation.
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        public static Hash XorAndCompute(Hash h1, Hash h2)
        {
            var newBytes = new byte[AElfConstants.HashByteArrayLength];
            for (var i = 0; i < newBytes.Length; i++) newBytes[i] = (byte)(h1.Value[i] ^ h2.Value[i]);

            return ComputeFrom(newBytes);
        }

        public static Hash ConcatAndCompute(Hash hash1, Hash hash2)
        {
            var bytes = ByteArrayHelper.ConcatArrays(hash1.ToByteArray(), hash2.ToByteArray());
            return ComputeFrom(bytes);
        }

        public static Hash ConcatAndCompute(Hash hash1, Hash hash2, Hash hash3)
        {
            var bytes = ByteArrayHelper.ConcatArrays(
                ByteArrayHelper.ConcatArrays(hash1.ToByteArray(), hash2.ToByteArray()), hash3.ToByteArray());
            return ComputeFrom(bytes);
        }
    }
}