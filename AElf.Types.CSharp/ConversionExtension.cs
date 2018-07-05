﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;

namespace AElf.Types.CSharp
{
    public static class ConversionExtension
    {
        private static byte[] TrimLeadingZeros(this byte[] bytes)
        {
            // TODO: Maybe improve performance
            return bytes.Skip(Array.FindIndex(bytes, Convert.ToBoolean)).ToArray();
        }
        
        #region bool
        public static bool DeserializeToBool(this ByteString bs)
        {
            return BoolValue.Parser.ParseFrom(bs).Value;
        }
        
        public static IMessage ToPbMessage(this bool value)
        {
            return new BoolValue() { Value = value };
        }

        public static Any ToAny(this bool value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static bool AnyToBool(this Any any)
        {
            return any.Unpack<BoolValue>().Value;
        }
        #endregion bool
        #region int
        public static int DeserializeToInt32(this ByteString bs)
        {
            return SInt32Value.Parser.ParseFrom(bs).Value;
        }

        public static IMessage ToPbMessage(this int value)
        {
            return new SInt32Value() { Value = value };
        }

        public static Any ToAny(this int value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static int AnyToInt32(this Any any)
        {
            return any.Unpack<SInt32Value>().Value;
        }
        #endregion int
        #region uint
        public static uint DeserializeToUInt32(this ByteString bs)
        {
            return UInt32Value.Parser.ParseFrom(bs).Value;
        }

        public static byte[] ToFriendlyBytes(this uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes.TrimLeadingZeros();
        }
        
        public static IMessage ToPbMessage(this uint value)
        {
            return new UInt32Value() { Value = value };
        }

        public static Any ToAny(this uint value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static uint AnyToUInt32(this Any any)
        {
            return any.Unpack<UInt32Value>().Value;
        }
        #endregion uint
        #region long
        public static long DeserializeToInt64(this ByteString bs)
        {
            return SInt64Value.Parser.ParseFrom(bs).Value;
        }

        public static byte[] ToFriendlyBytes(this long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes.TrimLeadingZeros();
        }
        
        public static IMessage ToPbMessage(this long value)
        {
            return new SInt64Value() { Value = value };
        }

        public static Any ToAny(this long value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static long AnyToInt64(this Any any)
        {
            return any.Unpack<SInt64Value>().Value;
        }
        #endregion long
        #region ulong
        public static ulong DeserializeToUInt64(this ByteString bs)
        {
            return UInt64Value.Parser.ParseFrom(bs).Value;
        }

        public static byte[] ToFriendlyBytes(this ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes.TrimLeadingZeros();
        }
        
        public static IMessage ToPbMessage(this ulong value)
        {
            return new UInt64Value() { Value = value };
        }

        public static Any ToAny(this ulong value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static ulong AnyToUInt64(this Any any)
        {
            return any.Unpack<UInt64Value>().Value;
        }
        #endregion ulong
        #region string

        public static string DeserializeToString(this ByteString bs)
        {
            return StringValue.Parser.ParseFrom(bs).Value;
        }
        
        public static byte[] ToFriendlyBytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
        
        public static IMessage ToPbMessage(this string value)
        {
            return new StringValue() { Value = value };
        }

        public static Any ToAny(this string value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static string AnyToString(this Any any)
        {
            return any.Unpack<StringValue>().Value;
        }
        #endregion string
        #region byte[]

        public static byte[] DeserializeToBytes(this ByteString bs)
        {
            return BytesValue.Parser.ParseFrom(bs).Value.ToByteArray();
        }
       
        public static byte[] ToFriendlyBytes(this byte[] value)
        {
            return value;
        }
        
        public static IMessage ToPbMessage(this byte[] value)
        {
            return new BytesValue() { Value = ByteString.CopyFrom(value) };
        }

        public static Any ToAny(this byte[] value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static byte[] AnyToBytes(this Any any)
        {
            return any.Unpack<BytesValue>().Value.ToByteArray();
        }
        #endregion byte[]
        #region IMessage
        public static T DeserializeToPbMessage<T>(this byte[] bytes) where T : IMessage, new()
        {
            var obj = new T();
            ((IMessage)obj).MergeFrom(bytes);
            return obj;
        }
        
        public static T DeserializeToPbMessage<T>(this ByteString bs) where T : IMessage, new()
        {
            return bs.ToByteArray().DeserializeToPbMessage<T>();
        }

        public static byte[] ToFriendlyBytes(this IMessage value)
        {
            return value.ToByteArray();
        }
        
        public static IMessage ToPbMessage(this IMessage value)
        {
            return value;
        }

        public static Any ToAny(this IMessage value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static IMessage AnyToPbMessage(this Any any, System.Type type)
        {
            if (any == null)
            {
                throw new Exception($"Cannot convert null to {type.FullName}.");
            }

            if (!type.IsPbMessageType())
            {
                throw new Exception("Type given is not an IMessage.");
            }
            var target = (IMessage)Activator.CreateInstance(type);

            if (Any.GetTypeName(any.TypeUrl) != target.Descriptor.FullName)
            {
                throw new Exception(
                    $"Full type name for {target.Descriptor.Name} is {target.Descriptor.FullName}; Any message's type url is {any.TypeUrl}");
            }
            target.MergeFrom(any.Value);
            return target;
        }
        #endregion IMessage
        #region UserType
        public static T DeserializeToUserType<T>(this byte[] bytes) where T : UserType, new()
        {
            var obj = new T();
            var msg = new UserTypeHolder();
            msg.MergeFrom(bytes);
            obj.Unpack(msg);
            return obj;
        }
        
        public static T DeserializeToUserType<T>(this ByteString bs) where T : UserType, new()
        {
            return bs.ToByteArray().DeserializeToUserType<T>();
        }

        public static byte[] ToFriendlyBytes(this UserType value)
        {
            return value.ToPbMessage().ToByteArray();
        }
        
        public static IMessage ToPbMessage(this UserType value)
        {
            return value.Pack();
        }

        public static Any ToAny(this UserType value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static UserType AnyToUserType(this Any any, System.Type type)
        {
            if (!type.IsUserType())
                throw new Exception("Type given is not a UserType.");
            var obj = (UserType)Activator.CreateInstance(type);
            obj.Unpack(any.Unpack<UserTypeHolder>());
            return obj;
        }
        #endregion UserType
    }
}
