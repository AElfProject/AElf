using System.Diagnostics.CodeAnalysis;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public enum OpCodeEnum
{
    Beq,
    Beq_S,
    Bge,
    Bge_S,
    Bge_Un,
    Bge_Un_S,
    Bgt,
    Bgt_S,
    Bgt_Un,
    Bgt_Un_S,
    Ble,
    Ble_S,
    Ble_Un,
    Ble_Un_S,
    Blt,
    Blt_S,
    Blt_Un,
    Blt_Un_S,
    Bne_Un,
    Bne_Un_S,
    Br,
    Br_S,
    Brfalse,
    Brfalse_S,
    Brtrue,
    Brtrue_S
}

public class OpCodeFixtures
{
    public static readonly Dictionary<OpCodeEnum, OpCode> OpCodeLookup = new Dictionary<OpCodeEnum, OpCode>()
    {
        {OpCodeEnum.Beq, OpCodes.Beq},
        {OpCodeEnum.Beq_S, OpCodes.Beq_S},
        {OpCodeEnum.Bge, OpCodes.Bge},
        {OpCodeEnum.Bge_S, OpCodes.Bge_S},
        {OpCodeEnum.Bge_Un, OpCodes.Bge_Un},
        {OpCodeEnum.Bge_Un_S, OpCodes.Bge_Un_S},
        {OpCodeEnum.Bgt, OpCodes.Bgt},
        {OpCodeEnum.Bgt_S, OpCodes.Bgt_S},
        {OpCodeEnum.Bgt_Un, OpCodes.Bgt_Un},
        {OpCodeEnum.Bgt_Un_S, OpCodes.Bgt_Un_S},
        {OpCodeEnum.Ble, OpCodes.Ble},
        {OpCodeEnum.Ble_S, OpCodes.Ble_S},
        {OpCodeEnum.Ble_Un, OpCodes.Ble_Un},
        {OpCodeEnum.Ble_Un_S, OpCodes.Ble_Un_S},
        {OpCodeEnum.Blt, OpCodes.Blt},
        {OpCodeEnum.Blt_S, OpCodes.Blt_S},
        {OpCodeEnum.Blt_Un, OpCodes.Blt_Un},
        {OpCodeEnum.Blt_Un_S, OpCodes.Blt_Un_S},
        {OpCodeEnum.Bne_Un, OpCodes.Bne_Un},
        {OpCodeEnum.Bne_Un_S, OpCodes.Bne_Un_S},
        {OpCodeEnum.Br, OpCodes.Br},
        {OpCodeEnum.Br_S, OpCodes.Br_S},
        {OpCodeEnum.Brfalse, OpCodes.Brfalse},
        {OpCodeEnum.Brfalse_S, OpCodes.Brfalse_S},
        {OpCodeEnum.Brtrue, OpCodes.Brtrue},
        {OpCodeEnum.Brtrue_S, OpCodes.Brtrue_S},
    };

    public static readonly Dictionary<OpCode, OpCode> LongFormShortFormMap = new Dictionary<OpCode, OpCode>()
    {
        {OpCodes.Beq, OpCodes.Beq_S},
        {OpCodes.Bge, OpCodes.Bge_S},
        {OpCodes.Bge_Un, OpCodes.Bge_Un_S},
        {OpCodes.Bgt, OpCodes.Bgt_S},
        {OpCodes.Bgt_Un, OpCodes.Bgt_Un_S},
        {OpCodes.Ble, OpCodes.Ble_S},
        {OpCodes.Ble_Un, OpCodes.Ble_Un_S},
        {OpCodes.Blt, OpCodes.Blt_S},
        {OpCodes.Blt_Un, OpCodes.Blt_Un_S},
        {OpCodes.Bne_Un, OpCodes.Bne_Un_S},
        {OpCodes.Br, OpCodes.Br_S},
        {OpCodes.Brfalse, OpCodes.Brfalse_S},
        {OpCodes.Brtrue, OpCodes.Brtrue_S}
    };
}