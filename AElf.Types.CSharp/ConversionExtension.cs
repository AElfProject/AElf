using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;

namespace AElf.Types.CSharp
{
    public static class ConversionExtension
    {
        #region bool
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
        public static IMessage ToPbMessage(this int value)
        {
            return new Int32Value() { Value = value };
        }

        public static Any ToAny(this int value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static int AnyToInt32(this Any any)
        {
            return any.Unpack<Int32Value>().Value;
        }
        #endregion int
        #region uint
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
        public static IMessage ToPbMessage(this long value)
        {
            return new Int64Value() { Value = value };
        }

        public static Any ToAny(this long value)
        {
            return Any.Pack(value.ToPbMessage());
        }

        public static long AnyToInt64(this Any any)
        {
            return any.Unpack<Int64Value>().Value;
        }
        #endregion long
        #region ulong
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
