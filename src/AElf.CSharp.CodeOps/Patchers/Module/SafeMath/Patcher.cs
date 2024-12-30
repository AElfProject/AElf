using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps.Patchers.Module.SafeMath;

public class Patcher : IPatcher<ModuleDefinition>
{
    // Replace unchecked math OpCodes with checked OpCodes (overflow throws exception)
    private static readonly Dictionary<OpCode, OpCode> PlainToCheckedOpCodes = new()
    {
        {OpCodes.Add, OpCodes.Add_Ovf},
        {OpCodes.Sub, OpCodes.Sub_Ovf},
        {OpCodes.Mul, OpCodes.Mul_Ovf}
    };

    public bool SystemContactIgnored => false;

    public void Patch(ModuleDefinition module)
    {
        foreach (var method in module.GetAllTypes().SelectMany(t => t.Methods))
        {
            PatchMethod(method);
        }
    }

    private void PatchMethod(MethodDefinition method)
    {
        if (!method.HasBody)
            return;

        var processor = method.Body.GetILProcessor();
        processor.Body.SimplifyMacros();
        // Note: We have to convert it to List before iterating, otherwise it will be problematic when we iterate while rewriting
        var instructions = method.Body.Instructions.Where(i => PlainToCheckedOpCodes.ContainsKey(i.OpCode)).ToList();
        foreach (var instruction in instructions)
        {
            var checkedInstruction = processor.Create(PlainToCheckedOpCodes[instruction.OpCode]);
            processor.Replace(instruction, checkedInstruction);
        }
        processor.Body.OptimizeMacros();
    }
}