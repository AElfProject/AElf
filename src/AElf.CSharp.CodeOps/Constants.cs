using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps
{
    public static class Constants
    {
        public const int DefaultAuditTimeoutDuration = 60000;
        public const int MaxInheritanceThreshold = 5;

        public static readonly HashSet<OpCode> JumpingOpCodes = new HashSet<OpCode>
        {
            OpCodes.Brtrue,
            OpCodes.Brtrue_S,
            OpCodes.Br_S,
            OpCodes.Br
        };

        public static readonly HashSet<string> PrimitiveTypes = new HashSet<string>
        {
            typeof(bool).FullName,
            typeof(decimal).FullName,
            typeof(short).FullName,
            typeof(int).FullName,
            typeof(long).FullName,
            typeof(ushort).FullName,
            typeof(uint).FullName,
            typeof(ulong).FullName,
            typeof(string).FullName,
        };
    }
}