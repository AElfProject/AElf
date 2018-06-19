using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public static class TagExtension
    {
        private static readonly HashSet<Type> VarintTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };

        public static uint GetTagForFieldNumber( this object o, int fieldNumber)
        {
            // Support upto 63 fields first
            if (fieldNumber > 63)
            {
                throw  new Exception("Only upto 63 fields are supported.");
            }
            
            if (VarintTypes.Contains(o.GetType()))
            {
                return WireFormat.MakeTag(fieldNumber, WireFormat.WireType.Varint);
            }

            return WireFormat.MakeTag(fieldNumber, WireFormat.WireType.LengthDelimited);
        }
    }
}