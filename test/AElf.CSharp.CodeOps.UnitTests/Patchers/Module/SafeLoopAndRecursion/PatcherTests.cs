using AElf.CSharp.CodeOps.Patchers.Module.CallAndBranchCounts;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Patchers.Module.SafeLoopAndRecursion;

public class PatcherTests : CSharpCodeOpsTestBase
{
    [Theory]
    [InlineData(
        OpCodeEnum.Beq,
        "int num = 1; while (num == 1) {DummyMethod();}",
        "int num = 1; while (num == 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Beq_S,
        "int num = 1; while (num == 1) {DummyMethod();}",
        "int num = 1; while (num == 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bge,
        "int num = 1; while (num >= 1) {DummyMethod();}",
        "int num = 1; while (num >= 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bge_S,
        "int num = 1; while (num >= 1) {DummyMethod();}",
        "int num = 1; while (num >= 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"        
    )]
    [InlineData(
        OpCodeEnum.Bge_Un,
        "uint num = 1u; while (num >= 1) {DummyMethod();}",
        "uint num = 1u; while (num >= 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bge_Un_S,
        "uint num = 1u; while (num >= 1) {DummyMethod();}",
        "uint num = 1u; while (num >= 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bgt,
        "int num = 1; while (num > 1) {DummyMethod();}",
        "int num = 1; while (num > 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bgt_S,
        "int num = 1; while (num > 1) {DummyMethod();}",
        "int num = 1; while (num > 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bgt_Un,
        "uint num = 1u; while (num > 1) {DummyMethod();}",
        "uint num = 1u; while (num > 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bgt_Un_S,
        "uint num = 1u; while (num > 1) {DummyMethod();}",
        "uint num = 1u; while (num > 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Ble,
        "int num = 1; while (num <= 1) {DummyMethod();}",
        "int num = 1; while (num <= 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Ble_S,
        "int num = 1; while (num <= 1) {DummyMethod();}",
        "int num = 1; while (num <= 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Ble_Un,
        "uint num = 1u; while (num <= 1) {DummyMethod();}",
        "uint num = 1u; while (num <= 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Ble_Un_S,
        "uint num = 1u; while (num <= 1) {DummyMethod();}",
        "uint num = 1u; while (num <= 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Blt,
        "int num = 1; while (num < 1) {DummyMethod();}",
        "int num = 1; while (num < 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Blt_S,
        "int num = 1; while (num < 1) {DummyMethod();}",
        "int num = 1; while (num < 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Blt_Un,
        "uint num = 1u; while (num < 1) {DummyMethod();}",
        "uint num = 1u; while (num < 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Blt_Un_S,
        "uint num = 1u; while (num < 1) {DummyMethod();}",
        "uint num = 1u; while (num < 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bne_Un,
        "uint num = 1u; while (num != 1) {DummyMethod();}",
        "uint num = 1u; while (num != 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Bne_Un_S,
        "uint num = 1u; while (num != 1) {DummyMethod();}",
        "uint num = 1u; while (num != 1) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Br,
        "while (true) {DummyMethod();}",
        "while (true) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Br_S,
        "while (true) {DummyMethod();}",
        "while (true) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Brfalse,
        "bool flag = false; while(!flag) {DummyMethod();}",
        "bool flag = false; while(!flag) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Brfalse_S,
        "bool flag = false; while(!flag) {DummyMethod();}",
        "bool flag = false; while(!flag) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Brtrue,
        "bool flag = true; while(flag) {DummyMethod();}",
	    "bool flag = true; while(flag) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    [InlineData(
        OpCodeEnum.Brtrue_S,
        "bool flag = true; while(flag) {DummyMethod();}",
	    "bool flag = true; while(flag) {ExecutionObserverProxy.BranchCount(); DummyMethod();}"
    )]
    public void Patch_Branch(OpCodeEnum opCodeEnum, string originalMethodBody, string patchedMethodBody)
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
				" + originalMethodBody + @"
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
				" + patchedMethodBody + @"
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodeLookup[opCodeEnum]);
    }

    #region Helpers

    private static ModuleDefinition ApplyPatch(ModuleDefinition module)
    {
        var patcher = new Patcher();
        patcher.Patch(module);
        return module;
    }

    private void DoTest(string method, string expectedContractCode, OpCode opCode)
    {
        var methodName = "InfiniteLoop";
        var builder = new SourceCodeBuilder("TestContract").AddMethod(method);
        var module = CompileToAssemblyDefinition(builder.Build()).MainModule;
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(x => x.Name == methodName);
        ReplaceOpCodeIfNeeded(methodDefinition, opCode);
        AssertMethodHasOpCode(module, methodName, opCode);
        ApplyPatch(module);
        var patchedCode =
            DecompileType(module.GetAllTypes().Single(t => t.FullName == builder.ContractTypeFullName));
        Assert.Equal(expectedContractCode.CleanCode(), patchedCode.CleanCode());
    }

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

    private static readonly Dictionary<OpCodeEnum, OpCode> OpCodeLookup = new Dictionary<OpCodeEnum, OpCode>()
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

    private static readonly Dictionary<OpCode, OpCode> LongFormShortFormMap = new Dictionary<OpCode, OpCode>()
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

    private static void ReplaceOpCodeIfNeeded(MethodDefinition method, OpCode opCode)
    {
        var isLongForm = LongFormShortFormMap.TryGetValue(opCode, out var shortFormOpCode);
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

    private void AssertMethodHasOpCode(ModuleDefinition module, string method, OpCode opCode)
    {
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(x => x.Name == method);
        Assert.True(methodDefinition.HasBody);
        Assert.Contains(methodDefinition.Body.Instructions, ins => ins.OpCode == opCode);
    }

    #endregion
}