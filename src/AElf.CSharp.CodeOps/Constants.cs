using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps;

public static class Constants
{
    public const int DefaultAuditTimeoutDuration = 60000;
    public const int MaxInheritanceThreshold = 5;

    public static readonly HashSet<OpCode> JumpingOpCodes = new()
    {
        OpCodes.Beq,
        OpCodes.Beq_S,
        OpCodes.Bge,
        OpCodes.Bge_S,
        OpCodes.Bge_Un,
        OpCodes.Bge_Un_S,
        OpCodes.Bgt,
        OpCodes.Bgt_S,
        OpCodes.Bgt_Un,
        OpCodes.Bgt_Un_S,
        OpCodes.Ble,
        OpCodes.Ble_S,
        OpCodes.Ble_Un,
        OpCodes.Ble_Un_S,
        OpCodes.Blt,
        OpCodes.Blt_S,
        OpCodes.Blt_Un,
        OpCodes.Blt_Un_S,
        OpCodes.Bne_Un,
        OpCodes.Bne_Un_S,
        OpCodes.Br,
        OpCodes.Br_S,
        OpCodes.Brfalse,
        OpCodes.Brfalse_S,
        OpCodes.Brtrue,
        OpCodes.Brtrue_S,
    };

    public static readonly HashSet<string> PrimitiveTypes = new()
    {
        typeof(bool).FullName,
        typeof(decimal).FullName,
        typeof(short).FullName,
        typeof(int).FullName,
        typeof(long).FullName,
        typeof(ushort).FullName,
        typeof(uint).FullName,
        typeof(ulong).FullName,
        typeof(string).FullName
    };
}