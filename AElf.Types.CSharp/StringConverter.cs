﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;

namespace AElf.Types.CSharp
{
    public static class StringConverter
    {
        private static readonly Dictionary<string, Type> _nameToType;

        private static readonly Dictionary<Type, Func<string, object>> StringHandlers =
            new Dictionary<Type, Func<string, object>>
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
                {typeof(Address), Address.Parse},
                {typeof(MerklePath), s => MerklePath.Parser.ParseFrom(ByteArrayHelpers.FromHexString(s))},
                {typeof(Authorization), s => Authorization.Parser.ParseFrom(ByteArrayHelpers.FromHexString(s))},
                {typeof(Proposal), s => Proposal.Parser.ParseFrom(ByteArrayHelpers.FromHexString(s))},
                {typeof(Timestamp), s => Timestamp.Parser.ParseFrom(ByteArrayHelpers.FromHexString(s))},
                {typeof(Approval), s => Approval.Parser.ParseFrom(ByteArrayHelpers.FromHexString(s))}
            };


        private static readonly Dictionary<Type, Func<object, string>> ObjectHandlers =
            new Dictionary<Type, Func<object, string>>
            {
                {typeof(bool), obj => ((bool) obj).ToString()},
                {typeof(int), obj => ((int) obj).ToString()},
                {typeof(uint), obj => ((uint) obj).ToString()},
                {typeof(long), obj => ((long) obj).ToString()},
                {typeof(ulong), obj => ((ulong) obj).ToString()},
                {typeof(string), obj => (string) obj},
                {typeof(byte[]), obj => ((byte[]) obj).ToHex()},
                {typeof(Hash), obj => ((Hash) obj).ToHex()},
                {typeof(Address), obj => ((Address) obj).GetFormatted()},
                {typeof(Timestamp), obj => ((Timestamp) obj).ToByteArray().ToHex()}
            };

        static StringConverter()
        {
            _nameToType = new Dictionary<string, Type>();
            foreach (var t in ObjectHandlers.Keys)
            {
                _nameToType.Add(t.FullName, t);
                var shortName = t.FullName.ToShorterName();
                if (shortName.Equals(t.FullName))
                    continue;
                _nameToType.Add(shortName, t);
            }
        }

        private static bool IsHex(string value)
        {
            return value.Length >= 2 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X');
        }

        private static Dictionary<string, Type> GetTypeLookup(IEnumerable<Type> types)
        {
            return types.Distinct().ToDictionary(t => t.FullName.ToShorterName(), t => t);
        }

        //TODO: Add GetTypeParser test case [Case]
        public static Func<string, object> GetTypeParser(string typeName, IEnumerable<Type> types = null)
        {
            if (_nameToType.TryGetValue(typeName, out var type))
                if (StringHandlers.TryGetValue(type, out var parser))
                    return parser;

            if (types != null)
            {
                var injected = GetTypeLookup(types);
                if (injected.TryGetValue(typeName, out type))
                    if (type.IsPbMessageType())
                        return s =>
                        {
                            var obj = (IMessage) Activator.CreateInstance(type);
                            return JsonParser.Default.Parse(s, obj.Descriptor);
                        };
            }

            throw new Exception($"Not Found parser for type {typeName}");
        }

        //TODO: Add GetTypeFormatter test case [Case]
        public static Func<object, string> GetTypeFormatter(string typeName, IEnumerable<Type> types = null)
        {
            if (_nameToType.TryGetValue(typeName, out var type))
                if (ObjectHandlers.TryGetValue(type, out var parser))
                {
                    if (type != typeof(bool)) return o => $@"""{parser(o)}""";

                    return o => parser(o).ToLower();
                }

            if (types != null)
            {
                var injected = GetTypeLookup(types);
                if (injected.TryGetValue(typeName, out type))
                    if (type.IsPbMessageType())
                        return o => JsonFormatter.Default.Format((IMessage) o);
            }

            throw new InvalidCastException($"Not Found parser for type {typeName}");
        }
    }
}