﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
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

        private static readonly Dictionary<System.Type, Func<string, object>> _handlers =
            new Dictionary<System.Type, Func<string, object>>()
            {
                {typeof(bool), (s) => bool.Parse(s)},
                {typeof(int), (s) => IsHex(s) ? int.Parse(s.Substring(2), NumberStyles.HexNumber) : int.Parse(s)},
                {typeof(uint), (s) => IsHex(s) ? uint.Parse(s.Substring(2), NumberStyles.HexNumber) : uint.Parse(s)},
                {typeof(long), (s) => IsHex(s) ? long.Parse(s.Substring(2), NumberStyles.HexNumber) : long.Parse(s)},
                {typeof(ulong), (s) => IsHex(s) ? ulong.Parse(s.Substring(2), NumberStyles.HexNumber) : ulong.Parse(s)},
                {typeof(string), (s) => s},
                {
                    typeof(byte[]),
                    (s) => Enumerable.Range(0, s.Length).Where(x => x % 2 == 0 && !(IsHex(s) && x == 0))
                        .Select(x => Convert.ToByte(s.Substring(x, 2), 16)).ToArray()
                }
            };

        static StringInputParsers()
        {
            _nameToType = new Dictionary<string, System.Type>();
            foreach (var t in _handlers.Keys)
            {
                _nameToType.Add(t.FullName, t);
                _nameToType.Add(t.FullName.ToShorterName(), t);
            }
        }

        public static Func<string, object> GetStringParserFor(string typeName)
        {
            if (_nameToType.TryGetValue(typeName, out var type))
            {
                if (_handlers.TryGetValue(type, out var parser))
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
                    if (length != 36)
                    {
                        throw new Exception("Hash has to be a byte array of fixed length.");
                    }

                    // Note: Hash has the same structure as BytesValue, hence using BytesValue for serialization.
                    // So that we don't need dependency AElf.Kernel.
                    
                    return new BytesValue()
                    {
                        Value = ByteString.CopyFrom((byte[]) _handlers[typeof(byte[])](s))
                    };
                };
            }

            throw new Exception($"Cannot find parser for type {typeName}");
        }
    }
}