using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps;

public static class Extensions
{
    public static string CleanCode(this string originalCode)
    {
        var code = originalCode.Replace("\r\n", "\n");
        code = Regex.Replace(code, "\n+", "\n", RegexOptions.Multiline);
        code = Regex.Replace(code, "\\s+", "");
        return code.Trim();
    }
    public static void AssertMethodHasOpCode(this ModuleDefinition module, string method, OpCode opCode)
    {
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(x => x.Name == method);
        Assert.True(methodDefinition.HasBody);
        Assert.Contains(methodDefinition.Body.Instructions, ins => ins.OpCode == opCode);
    }
    public static void MaybeReplaceShortFormOpCodeWithLongForm(this MethodDefinition method, OpCode opCode)
    {
        var isLongForm = OpCodeFixtures. LongFormShortFormMap.TryGetValue(opCode, out var shortFormOpCode);
        if (!isLongForm) return;
        var needReplacement = method.HasBody && method.Body.Instructions.Any(ins => ins.OpCode == shortFormOpCode);
        if (!needReplacement) return;

        var processor = method.Body.GetILProcessor();
        processor.Body.SimplifyMacros();
        var instructions = method.Body.Instructions.Where(i => i.OpCode == shortFormOpCode).ToList();
        // Possible to be empty as already converted in SimplifyMacros
        foreach (var instruction in instructions)
        {
            var longForm = processor.Create(shortFormOpCode, (Instruction) instruction.Operand);
            processor.Replace(instruction, longForm);
        }
    }
}