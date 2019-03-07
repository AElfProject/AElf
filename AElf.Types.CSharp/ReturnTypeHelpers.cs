using System;
using System.IO;
using System.Text;
using System.Web;
using AElf.Common;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public static class ReturnTypeHelper
    {
        private static byte[] EncodeByProtobuf<T>(T value, Action<CodedOutputStream, T> encoder)
        {
            using (var mm = new MemoryStream())
            using (var stream = new CodedOutputStream(mm))
            {
                encoder(stream, value);
                stream.Flush();
                mm.Position = 0;
                return mm.ToArray();
            }
        }

        private static T DecodeByProtobuf<T>(byte[] bytes, Func<CodedInputStream, T> decoder)
        {
            using (var stream = new CodedInputStream(bytes))
            {
                return decoder(stream);
            }
        }

        public static Func<T, byte[]> GetEncoder<T>()
        {
            var type = typeof(T);
            if (type == typeof(bool))
            {
                return value => EncodeByProtobuf(value,
                    (s, v) =>
                    {
                        dynamic vv = v;
                        s.WriteBool(vv);
                    });
            }

            if (type == typeof(int))
            {
                return value => EncodeByProtobuf(value,
                    (s, v) =>
                    {
                        dynamic vv = v;
                        s.WriteSInt32(vv);
                    });
            }

            if (type == typeof(uint))
            {
                return value => EncodeByProtobuf(value,
                    (s, v) =>
                    {
                        dynamic vv = v;
                        s.WriteUInt32(vv);
                    });
            }

            if (type == typeof(long))
            {
                return value => EncodeByProtobuf(value,
                    (s, v) =>
                    {
                        dynamic vv = v;
                        s.WriteSInt64(vv);
                    });
            }

            if (type == typeof(ulong))
            {
                return value => EncodeByProtobuf(value,
                    (s, v) =>
                    {
                        dynamic vv = v;
                        s.WriteUInt64(vv);
                    });
            }

            if (type == typeof(string))
            {
                return value => Encoding.UTF8.GetBytes(value.ToString());
            }

            if (type == typeof(byte[]))
            {
                return value =>
                {
                    dynamic vv = value;
                    return vv;
                };
            }

            if (type.IsPbMessageType())
            {
                return value => (value as IMessage)?.ToByteArray();
            }

            if (type.IsUserType())
            {
                return value => (value as UserType)?.Pack()?.ToByteArray();
            }

            throw new NotSupportedException($"Return type {type} is not supported.");
        }

        public static Func<byte[], T> GetDecoder<T>()
        {
            var type = typeof(T);
            if (type == typeof(bool))
            {
                return bytes => DecodeByProtobuf(bytes,
                    s =>
                    {
                        dynamic o = s.ReadBool();
                        return o;
                    });
            }

            if (type == typeof(int))
            {
                return bytes => DecodeByProtobuf(bytes,
                    s =>
                    {
                        dynamic o = s.ReadSInt32();
                        return o;
                    });
            }

            if (type == typeof(uint))
            {
                return bytes => DecodeByProtobuf(bytes,
                    s =>
                    {
                        dynamic o = s.ReadUInt32();
                        return o;
                    });
            }

            if (type == typeof(long))
            {
                return bytes => DecodeByProtobuf(bytes,
                    s =>
                    {
                        dynamic o = s.ReadSInt64();
                        return o;
                    });
            }

            if (type == typeof(ulong))
            {
                return bytes => DecodeByProtobuf(bytes,
                    s =>
                    {
                        dynamic o = s.ReadUInt64();
                        return o;
                    });
            }

            if (type == typeof(string))
            {
                return bytes =>
                {
                    dynamic o = Encoding.UTF8.GetString(bytes);
                    return o;
                };
            }

            if (type == typeof(byte[]))
            {
                return bytes =>
                {
                    dynamic o = bytes;
                    return o;
                };
            }

            if (type.IsPbMessageType())
            {
                return bytes =>
                {
                    dynamic o = Activator.CreateInstance<T>();
                    (o as IMessage).MergeFrom(bytes);
                    return o;
                };
            }

            if (type.IsUserType())
            {
                return bytes =>
                {
                    dynamic o = Activator.CreateInstance<T>();
                    var holder = new UserTypeHolder();
                    holder.MergeFrom(bytes);
                    ((UserType) o).Unpack(holder);
                    return o;
                };
            }

            throw new NotSupportedException($"Return type {type} is not supported.");
        }

        public static Func<T, string> GetStringConverter<T>()
        {
            var type = typeof(T);
            if (type == typeof(bool))
            {
                return v =>
                {
                    dynamic vv = v;
                    return (bool) vv ? "true" : "false";
                };
            }

            if (type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
                type == typeof(string))
            {
                return v =>
                {
                    dynamic vv = v;
                    return $@"""{HttpUtility.JavaScriptStringEncode(vv.ToString())}""";
                };
            }

            if (type == typeof(byte[]))
            {
                return v =>
                {
                    dynamic vv = v;
                    return $@"""{HttpUtility.JavaScriptStringEncode(((byte[]) vv).ToHex())}""";
                };
            }

            if (type.IsPbMessageType())
            {
                return v =>
                {
                    dynamic vv = v;
                    return vv.ToString();
                };
            }

            if (type.IsUserType())
            {
                return v =>
                {
                    dynamic vv = v;
                    return ((UserType) vv).Pack().ToString();
                };
            }

            throw new NotSupportedException($"Return type {type} is not supported.");
        }
    }
}