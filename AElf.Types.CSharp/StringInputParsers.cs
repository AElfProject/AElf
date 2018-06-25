using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using AElf.Kernel;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public static class StringInputParsers
    {
        private static readonly Dictionary<string, Type> _nameToType;

        private static bool IsHex(string value)
        {
            return value.Length >= 2 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X');
        }

        private static readonly Dictionary<Type, Func<string, object>> _handlers = new Dictionary<Type, Func<string, object>>(){
            {typeof(bool), (s)=>bool.Parse(s)},
            {typeof(int), (s)=>IsHex(s)?int.Parse(s.Substring(2),NumberStyles.HexNumber):int.Parse(s)},
            {typeof(uint), (s)=>IsHex(s)?uint.Parse(s.Substring(2), NumberStyles.HexNumber):uint.Parse(s)},
            {typeof(long), (s)=>IsHex(s)?long.Parse(s.Substring(2), NumberStyles.HexNumber):long.Parse(s)},
            {typeof(ulong), (s)=>IsHex(s)?ulong.Parse(s.Substring(2), NumberStyles.HexNumber):ulong.Parse(s)},
            {typeof(string), (s)=>s},
            {typeof(byte[]), (s)=>Enumerable.Range(0, s.Length).Where(x=>x%2==0&&!(IsHex(s)&&x==0)).Select(x=>Convert.ToByte(s.Substring(x,2), 16)).ToArray()}
        };

        static StringInputParsers()
        {
            _nameToType = new Dictionary<string, Type>();
            foreach (var t in _handlers.Keys)
            {
                _nameToType.Add(t.FullName, t);
                _nameToType.Add(t.FullName.ToShorterName(), t);
            }
            _nameToType.Add(typeof(Hash).FullName, typeof(Hash));
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
            if (typeName == typeof(Hash).FullName)
            {
                return (s) =>
                {
                    var length = s.Length;
                    length -= IsHex(s) ? 2 : 0;
                    if (length != 64)
                    {
                        throw new Exception("Hash has to be a byte array of length 64.");
                    }
                    return new Hash() { Value = ByteString.CopyFrom((byte[])_handlers[typeof(byte[])](s)) };
                };
            }
            throw new Exception($"Cannot find parser for type {typeName}");
        }
    }
}
