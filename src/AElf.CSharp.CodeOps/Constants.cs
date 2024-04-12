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

    /// <summary>
    /// The compiler will generate static fields for LINQ functions. We allow those static fields and won't reset them.
    /// </summary>
    public static HashSet<string> FuncTypes = new ()
    {
        "System.Func`1", "System.Func`2", "System.Func`3", "System.Func`4", "System.Func`5", "System.Func`6",
        "System.Func`7", "System.Func`8", "System.Func`9", "System.Func`10", "System.Func`11", "System.Func`12",
        "System.Func`13", "System.Func`14", "System.Func`15", "System.Func`16", "System.Func`17"
    };
}