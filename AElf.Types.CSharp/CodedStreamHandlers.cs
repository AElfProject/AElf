using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public static class CodedStreamHandlers
    {
        private static readonly Dictionary<Type, Action<CodedOutputStream, object>> _writeHandlers =
            new Dictionary<Type, Action<CodedOutputStream, object>>()
            {
                {typeof(bool), (s, v) => s.WriteBool((bool) v)},
                {typeof(int), (s, v) => s.WriteSInt32((int) v)},
                {typeof(uint), (s, v) => s.WriteUInt32((uint) v)},
                {typeof(long), (s, v) => s.WriteSInt64((long) v)},
                {typeof(ulong), (s, v) => s.WriteUInt64((ulong) v)},
                {typeof(string), (s, v) => s.WriteString((string) v)},
                {typeof(byte[]), (s, v) => s.WriteBytes(ByteString.CopyFrom((byte[]) v))}
            };

        private static readonly Dictionary<Type, Func<CodedInputStream, object>> _readHandlers =
            new Dictionary<Type, Func<CodedInputStream, object>>()
            {
                {typeof(bool), (s) => s.ReadBool()},
                {typeof(int), (s) => s.ReadSInt32()},
                {typeof(uint), (s) => s.ReadUInt32()},
                {typeof(long), (s) => s.ReadSInt64()},
                {typeof(ulong), (s) => s.ReadUInt64()},
                {typeof(string), (s) => s.ReadString()},
                {typeof(byte[]), (s) => s.ReadBytes().ToByteArray()}
            };

        public static void WriteToStream(this object value, CodedOutputStream output)
        {
            var type = value.GetType();
            if (_writeHandlers.TryGetValue(type, out var h))
            {
                h(output, value);
            }
            else if (type.IsPbMessageType())
            {
                output.WriteMessage((IMessage)value);
            }
            else if (type.IsUserType())
            {
                output.WriteMessage(((UserType)value).ToPbMessage());
            }
            else
            {
                throw new Exception($"Unable to write for type {type}.");
            }
        }

        public static object GetDefault(this Type type)
        {
            if(type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
        
        public static object ReadFromStream(this Type type, CodedInputStream input)
        {
            if (_readHandlers.TryGetValue(type, out var h))
            {
                return h(input);
            }
            else if (type.IsPbMessageType())
            {
                var obj = (IMessage)Activator.CreateInstance(type);
                input.ReadMessage(obj);
                return obj;
            }
            else if (type.IsUserType())
            {
                var obj = (UserType)Activator.CreateInstance(type);
                var holder = new UserTypeHolder();
                input.ReadMessage(holder);
                obj.Unpack(holder);
                return obj;
            }
            else
            {
                throw new Exception($"Unable to read for type {type}.");
            }
        }
    }
}