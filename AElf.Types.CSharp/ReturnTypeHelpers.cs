using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public static class ReturnTypeHelper
    {
        private static byte[] EncodeByProtobuf<T>(T value, Action<CodedOutputStream, T> encoder)
        {
            if (EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                return null;
            }

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
                return value =>
                {
                    if (value == null)
                    {
                        return null;
                    }

                    return Encoding.UTF8.GetBytes(value.ToString());
                };
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
                return value =>
                {
                    if (value == null)
                    {
                        return null;
                    }

                    return (value as IMessage)?.ToByteArray();
                };
            }

            if (type.IsUserType())
            {
                return value =>
                {
                    if (value == null)
                    {
                        return null;
                    }

                    var holder = (UserTypeHolder) type.GetMethod("Pack").Invoke(value, new object[0]);
                    return holder.ToByteArray();
                };
            }

            throw new NotSupportedException($"Return type {type} is not supported.");
        }

        public static Func<byte[], T> GetDecoder<T>()
        {
            var type = typeof(T);
            if (type == typeof(bool))
            {
                return bytes =>
                {
                    if (bytes == null)
                    {
                        dynamic d = false;
                        return d;
                    }

                    return DecodeByProtobuf(bytes,
                        s =>
                        {
                            dynamic o = s.ReadBool();
                            return o;
                        });
                };
            }

            if (type == typeof(int))
            {
                return bytes =>
                {
                    if (bytes == null)
                    {
                        dynamic d = 0;
                        return d;
                    }

                    return DecodeByProtobuf(bytes,
                        s =>
                        {
                            dynamic o = s.ReadSInt32();
                            return o;
                        });
                };
            }

            if (type == typeof(uint))
            {
                return bytes =>
                {
                    if (bytes == null)
                    {
                        dynamic d = 0U;
                        return d;
                    }

                    return DecodeByProtobuf(bytes,
                        s =>
                        {
                            dynamic o = s.ReadUInt32();
                            return o;
                        });
                };
            }

            if (type == typeof(long))
            {
                return bytes =>
                {
                    if (bytes == null)
                    {
                        dynamic d = 0L;
                        return d;
                    }

                    return DecodeByProtobuf(bytes,
                        s =>
                        {
                            dynamic o = s.ReadSInt64();
                            return o;
                        });
                };
            }

            if (type == typeof(ulong))
            {
                return bytes =>
                {
                    if (bytes == null)
                    {
                        dynamic d = 0UL;
                        return d;
                    }

                    return DecodeByProtobuf(bytes, s =>
                    {
                        dynamic o = s.ReadUInt64();
                        return o;
                    });
                };
            }

            if (type == typeof(string))
            {
                return bytes =>
                {
                    if (bytes == null)
                    {
                        return default(T);
                    }

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
                    if (bytes == null)
                    {
                        return default(T);
                    }

                    dynamic o = Activator.CreateInstance<T>();
                    (o as IMessage).MergeFrom(bytes);
                    return o;
                };
            }

            if (type.IsUserType())
            {
                return bytes =>
                {
                    if (bytes == null)
                    {
                        return default(T);
                    }

                    dynamic o = Activator.CreateInstance<T>();
                    var holder = new UserTypeHolder();
                    holder.MergeFrom(bytes);
                    typeof(T).GetMethod("Unpack").Invoke(o,new object[]{holder});
                    return (T) o;
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

            if (type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            {
                return v =>
                {
                    dynamic vv = v;
                    return vv.ToString();
                };
            }

            if (type == typeof(string))
            {
                return v =>
                {
                    dynamic vv = v;
                    return vv;
                };
            }

            if (type == typeof(byte[]))
            {
                return v =>
                {
                    if (v == null)
                    {
                        return null;
                    }

                    dynamic vv = v;
                    return ((byte[]) vv).ToHex();
                };
            }

            if (type.IsPbMessageType())
            {
                return v =>
                {
                    if (v == null)
                    {
                        return null;
                    }

                    dynamic vv = v;
                    return vv.ToString();
                };
            }

            if (type.IsUserType())
            {
                return v =>
                {
                    if (v == null)
                    {
                        return null;
                    }

                    dynamic vv = v;
                    return ((UserType) vv).Pack().ToString();
                };
            }

            throw new NotSupportedException($"Return type {type} is not supported.");
        }
    }
}