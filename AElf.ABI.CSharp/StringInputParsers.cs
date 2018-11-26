using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ABI.CSharp
{
    public static class StringInputParsers
    {
        private static readonly Dictionary<string, System.Type> _nameToType;

        private static bool IsHex(string value)
        {
            return value.Length >= 2 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X');
        }

        private static readonly Dictionary<System.Type, Func<string, object>> StringHandlers =
            new Dictionary<System.Type, Func<string, object>>()
            {
                {typeof(bool), s => bool.Parse(s)},
                {typeof(int), s => IsHex(s) ? int.Parse(s.Substring(2), NumberStyles.HexNumber) : int.Parse(s)},
                {typeof(uint), s => IsHex(s) ? uint.Parse(s.Substring(2), NumberStyles.HexNumber) : uint.Parse(s)},
                {typeof(long), s => IsHex(s) ? long.Parse(s.Substring(2), NumberStyles.HexNumber) : long.Parse(s)},
                {typeof(ulong), s => IsHex(s) ? ulong.Parse(s.Substring(2), NumberStyles.HexNumber) : ulong.Parse(s)},
                {typeof(string), s => s},
                {
                    typeof(byte[]),
                    s => Enumerable.Range(0, s.Length).Where(x => x % 2 == 0 && !(IsHex(s) && x == 0))
                        .Select(x => Convert.ToByte(s.Substring(x, 2), 16)).ToArray()
                },
                {typeof(Hash), Hash.LoadHex},
                {typeof(Address), Address.LoadHex},
                {typeof(MerklePath), (s) => MerklePath.Parser.ParseFrom(ByteArrayHelpers.FromHexString(s))},
                {typeof(ParentChainBlockInfo), (s) => ParentChainBlockInfo.Parser.ParseFrom(ByteArrayHelpers.FromHexString(s))},
                {typeof(SideChainBlockInfo), (s) => SideChainBlockInfo.Parser.ParseFrom(ByteArrayHelpers.FromHexString(s))}
            };


        private static readonly Dictionary<System.Type, Func<object, string>> ObjectHandlers =
            new Dictionary<System.Type, Func<object, string>>()
            {
                {typeof(bool), obj => ((bool) obj).ToString()},
                {typeof(int), obj => ((int) obj).ToString()},
                {typeof(uint), obj => ((uint) obj).ToString()},
                {typeof(long), obj => ((long) obj).ToString()},
                {typeof(ulong), obj => ((ulong) obj).ToString()},
                {typeof(string), obj => (string) obj},
                {
                    typeof(byte[]),
                    obj => ((byte[]) obj).ToHex()
                },
                {typeof(Hash), obj=> ((Hash)obj).DumpHex()},
                {typeof(Address), obj=> ((Address)obj).DumpHex()},
                {typeof(MerklePath), obj => ((MerklePath) obj).ToByteArray().ToHex()},
                {typeof(ParentChainBlockInfo), obj => ((ParentChainBlockInfo) obj).ToByteArray().ToHex()},
                {typeof(SideChainBlockInfo), obj => ((SideChainBlockInfo) obj).ToByteArray().ToHex()}
            };

        static StringInputParsers()
        {
            _nameToType = new Dictionary<string, System.Type>();
            foreach (var t in StringHandlers.Keys)
            {
                _nameToType.Add(t.FullName, t);
                var shortName = t.FullName.ToShorterName();
                if (shortName.Equals(t.FullName))
                    continue;
                _nameToType.Add(shortName, t);
            }
        }

        public static Func<string, object> GetStringParserFor(string typeName)
        {
            if (_nameToType.TryGetValue(typeName, out var type))
            {
                if (StringHandlers.TryGetValue(type, out var parser))
                {
                    return parser;
                }
            }

            if (typeName == Globals.HASH_TYPE_FULL_NAME)
            {
                return (s) =>
                {
                    var length = s.Length;
                    length -= IsHex(s) ? 2 : 0;
                    if (length != 36 && length != 64)
                    {
                        throw new Exception("Hash has to be a byte array of fixed length.");
                    }

                    // Note: Hash has the same structure as BytesValue, hence using BytesValue for serialization.
                    // So that we don't need dependency AElf.Kernel.

                    return new BytesValue()
                    {
                        Value = ByteString.CopyFrom((byte[]) StringHandlers[typeof(byte[])](s))
                    };
                };
            }

            throw new Exception($"Not Found parser for type {typeName}");
        }


        public static Func<object, string> ParseToStringFor(string typeName)
        {
            if (_nameToType.TryGetValue(typeName, out var type))
            {
                if (ObjectHandlers.TryGetValue(type, out var parser))
                {
                    return parser;
                }
            }

            if (typeName == Globals.HASH_TYPE_FULL_NAME)
            {
                return obj => ((Hash) obj).DumpHex();
            }

            throw new Exception($"Not Found parser for type {typeName}");
        }
    }
}