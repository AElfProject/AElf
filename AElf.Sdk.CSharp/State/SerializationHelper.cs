using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AElf.Types.CSharp;
using Google.Protobuf;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace AElf.Sdk.CSharp.State
{
    public static class SerializationHelper
    {
        private static readonly Dictionary<Type, Action<CodedOutputStream, object>> _primitiveWriters =
            new Dictionary<Type, Action<CodedOutputStream, object>>()
            {
                {typeof(bool), (stream, value) => stream.WriteBool((bool) value)},
                {typeof(int), (stream, value) => stream.WriteSInt32((int) value)},
                {typeof(uint), (stream, value) => stream.WriteUInt32((uint) value)},
                {typeof(long), (stream, value) => stream.WriteSInt64((long) value)},
                {typeof(ulong), (stream, value) => stream.WriteUInt64((ulong) value)},
                {typeof(byte[]), (stream, value) => stream.WriteBytes(ByteString.CopyFrom((byte[]) value))},
            };

        private static readonly Dictionary<Type, Func<CodedInputStream, object>> _primitiveReaders =
            new Dictionary<Type, Func<CodedInputStream, object>>()
            {
                {typeof(bool), stream => stream.ReadBool()},
                {typeof(int), stream => stream.ReadSInt32()},
                {typeof(uint), stream => stream.ReadUInt32()},
                {typeof(long), stream => stream.ReadSInt64()},
                {typeof(ulong), stream => stream.ReadUInt64()},
                {typeof(byte[]), stream => stream.ReadBytes().ToByteArray()}
            };

        private static Func<object, byte[]> GetPrimitiveSerializer(Type type)
        {
            if (_primitiveWriters.TryGetValue(type, out var writer))
            {
                return value =>
                {
                    using (var mm = new MemoryStream())
                    using (var cos = new CodedOutputStream(mm))
                    {
                        writer(cos, value);
                        cos.Flush();
                        mm.Position = 0;
                        return mm.ToArray();
                    }
                };
            }

            return null;
        }

        private static Func<byte[], object> GetPrimitiveDeserializer(Type type)
        {
            if (_primitiveReaders.TryGetValue(type, out var reader))
            {
                return bytes =>
                {
                    using (var cis = new CodedInputStream(bytes))
                    {
                        return reader(cis);
                    }
                };
            }

            return null;
        }

        //Done: make a unit test to test Serialize / Deserialize different types such as int,string,long,Block,Hash....
        public static byte[] Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            var type = value.GetType();
            var primitiveSerializer = GetPrimitiveSerializer(type);
            if (primitiveSerializer != null)
            {
                return primitiveSerializer(value);
            }

            if (type == typeof(string))
            {
                return Encoding.UTF8.GetBytes((string) value);
            }

            if (typeof(IMessage).IsAssignableFrom(type))
            {
                var v = (IMessage) value;
                return v.ToByteArray();
            }

            throw new InvalidOperationException($"Invalid type {type}.");
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            if (bytes == null)
                return default(T);

            var type = typeof(T);
            var primitiveDeserializer = GetPrimitiveDeserializer(type);
            if (primitiveDeserializer != null)
            {
                if (bytes.Length > 0)
                {
                    return (T) primitiveDeserializer(bytes);
                }

                return default(T);
            }

            if (type == typeof(string))
            {
                return (T) (object) Encoding.UTF8.GetString(bytes);
            }

            if (typeof(IMessage).IsAssignableFrom(type))
            {
                var instance = (IMessage) Activator.CreateInstance(type);
                instance.MergeFrom(bytes);
                return (T) instance;
            }

            throw new InvalidOperationException($"Invalid type {type}.");
        }
    }
}