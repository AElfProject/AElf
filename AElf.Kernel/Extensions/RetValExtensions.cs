using System;
using System.Linq;
using System.Text;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Extensions
{
    public static class RetValExtensions
    {
        /// <summary>
        /// Converts the serialized protobuf data to human friendly representation. 
        /// </summary>
        /// <returns></returns>
        public static byte[] ToFriendlyBytes(this RetVal retVal)
        {
            switch (retVal.Type)
            {
                case RetVal.Types.RetType.Bool:
                    var boolval = new BoolValue();
                    ((IMessage) boolval).MergeFrom(retVal.Data);
                    return BitConverter.GetBytes(boolval.Value);
                case RetVal.Types.RetType.Int32:
                    var sint32Val = new SInt32Value();
                    ((IMessage) sint32Val).MergeFrom(retVal.Data);
                    return GetFriendlyBytes(sint32Val.Value);
                case RetVal.Types.RetType.Uint32:
                    var uint32Val = new UInt32Value();
                    ((IMessage) uint32Val).MergeFrom(retVal.Data);
                    return GetFriendlyBytes(uint32Val.Value);
                case RetVal.Types.RetType.Int64:
                    var sint64Val = new SInt64Value();
                    ((IMessage) sint64Val).MergeFrom(retVal.Data);
                    return GetFriendlyBytes(sint64Val.Value);
                case RetVal.Types.RetType.Uint64:
                    var uint64Val = new UInt64Value();
                    ((IMessage) uint64Val).MergeFrom(retVal.Data);
                    return GetFriendlyBytes(uint64Val.Value);
                case RetVal.Types.RetType.String:
                    var stringVal = new StringValue();
                    ((IMessage) stringVal).MergeFrom(retVal.Data);
                    return GetFriendlyBytes(stringVal.Value);
                case RetVal.Types.RetType.Bytes:
                    var bytesVal = new BytesValue();
                    ((IMessage) bytesVal).MergeFrom(retVal.Data);
                    return GetFriendlyBytes(bytesVal.Value.ToByteArray());
                case RetVal.Types.RetType.PbMessage:
                case RetVal.Types.RetType.UserType:
                    // Both are treated as bytes
                    return GetFriendlyBytes(retVal.ToByteArray());
            }

            return new byte[0];
        }

        #region Helpers

        private static byte[] GetFriendlyBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return TrimLeadingZeros(bytes);
        }

        private static byte[] GetFriendlyBytes(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return TrimLeadingZeros(bytes);
        }

        private static byte[] GetFriendlyBytes(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return TrimLeadingZeros(bytes);
        }

        private static byte[] GetFriendlyBytes(ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return TrimLeadingZeros(bytes);
        }

        private static byte[] GetFriendlyBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        private static byte[] GetFriendlyBytes(byte[] value)
        {
            return value;
        }

        private static byte[] TrimLeadingZeros(byte[] bytes)
        {
            // TODO: Maybe improve performance
            return bytes.Skip(Array.FindIndex(bytes, Convert.ToBoolean)).ToArray();
        }

        #endregion Helpers
    }
}