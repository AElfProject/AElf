using System;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;

namespace AElf.Kernel
{
    public partial class RetVal
    {
        /// <summary>
        /// Converts the serialized protobuf data to human friendly representation. 
        /// </summary>
        /// <returns></returns>
        public byte[] ToFriendlyBytes()
        {
            switch (Type)
            {
                case Types.RetType.Bool:
                    var boolval = new BoolValue();
                    ((IMessage) boolval).MergeFrom(this.Data);
                    return BitConverter.GetBytes(boolval.Value);
                case Types.RetType.Int32:
                    var sint32Val = new SInt32Value();
                    ((IMessage) sint32Val).MergeFrom(this.Data);
                    return GetFriendlyBytes(sint32Val.Value);
                case Types.RetType.Uint32:
                    var uint32Val = new UInt32Value();
                    ((IMessage) uint32Val).MergeFrom(this.Data);
                    return GetFriendlyBytes(uint32Val.Value);
                case Types.RetType.Int64:
                    var sint64Val = new SInt64Value();
                    ((IMessage) sint64Val).MergeFrom(this.Data);
                    return GetFriendlyBytes(sint64Val.Value);
                case Types.RetType.Uint64:
                    var uint64Val = new UInt64Value();
                    ((IMessage) uint64Val).MergeFrom(this.Data);
                    return GetFriendlyBytes(uint64Val.Value);
                case Types.RetType.String:
                    var stringVal = new StringValue();
                    ((IMessage) stringVal).MergeFrom(this.Data);
                    return GetFriendlyBytes(stringVal.Value);
                case Types.RetType.Bytes:
                    var bytesVal = new BytesValue();
                    ((IMessage) bytesVal).MergeFrom(this.Data);
                    return GetFriendlyBytes(bytesVal.Value.ToByteArray());
                case Types.RetType.PbMessage:
                case Types.RetType.UserType:
                    // Both are treated as bytes
                    return GetFriendlyBytes(Data.ToByteArray());
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