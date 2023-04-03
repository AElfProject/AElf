using System.Diagnostics.CodeAnalysis;
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
        DoTest(method, expectedPatchedContractCode, OpCodeFixtures.OpCodeLookup[opCodeEnum]);
    }

    [Fact]
    public void ObserverProxy_Type_Added()
    {
        const string typeNameToBeAdded = "ExecutionObserverProxy";
        var builder = new SourceCodeBuilder("TestContract");
        var module = CompileToAssemblyDefinition(builder.Build()).MainModule;
        var notHavingExecutionObserverProxyType = module.GetAllTypes().All(t => t.Name != typeNameToBeAdded);
        Assert.True(notHavingExecutionObserverProxyType);
        ApplyPatch(module);
        var hasExecutionObserverProxyType = module.GetAllTypes().Any(t => t.Name == typeNameToBeAdded);
        Assert.True(hasExecutionObserverProxyType);
        var addedType =
            DecompileType(module.GetAllTypes().Single(t => t.Name == typeNameToBeAdded));
        const string expectedAddedType = @"using System;
using System.Runtime.InteropServices;
using AElf.Kernel.SmartContract;

namespace TestContract;

public static class ExecutionObserverProxy
{
	[ThreadStatic]
	private static IExecutionObserver _observer;

	public static void SetObserver ([In] IExecutionObserver observer)
	{
		_observer = observer;
	}

	public static void BranchCount ()
	{
		if (_observer != null) {
			_observer.BranchCount ();
		}
	}

	public static void CallCount ()
	{
		if (_observer != null) {
			_observer.CallCount ();
		}
	}
}
";
        Assert.Equal(expectedAddedType.CleanCode(), addedType.CleanCode());
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
        methodDefinition.MaybeReplaceShortFormOpCodeWithLongForm(opCode);
        module.AssertMethodHasOpCode(methodName, opCode);
        ApplyPatch(module);
        var patchedCode =
            DecompileType(module.GetAllTypes().Single(t => t.FullName == builder.ContractTypeFullName));
        Assert.Equal(expectedContractCode.CleanCode(), patchedCode.CleanCode());
    }



    #endregion
}