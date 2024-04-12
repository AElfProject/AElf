using AElf.CSharp.CodeOps.Validators.Module;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Module.SafeLoopAndRecursion;

public class ObserverProxyValidatorTests : CSharpCodeOpsTestBase
{
    public static IEnumerable<object[]> TestData =>
        new List<object[]>
        {
            new object[] {OpCodeEnum.Beq, "int num = 1; while (num == 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Beq_S, "int num = 1; while (num == 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bge, "int num = 1; while (num >= 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bge_S, "int num = 1; while (num >= 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bge_Un, "uint num = 1u; while (num >= 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bge_Un_S, "uint num = 1u; while (num >= 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bgt, "int num = 1; while (num > 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bgt_S, "int num = 1; while (num > 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bgt_Un, "uint num = 1u; while (num > 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bgt_Un_S, "uint num = 1u; while (num > 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Ble, "int num = 1; while (num <= 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Ble_S, "int num = 1; while (num <= 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Ble_Un, "uint num = 1u; while (num <= 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Ble_Un_S, "uint num = 1u; while (num <= 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Blt, "int num = 1; while (num < 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Blt_S, "int num = 1; while (num < 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Blt_Un, "uint num = 1u; while (num < 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Blt_Un_S, "uint num = 1u; while (num < 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bne_Un, "uint num = 1u; while (num != 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Bne_Un_S, "uint num = 1u; while (num != 1) {DummyMethod();}"},
            new object[] {OpCodeEnum.Br, "while (true) {DummyMethod();}"},
            new object[] {OpCodeEnum.Br_S, "while (true) {DummyMethod();}"},
            new object[] {OpCodeEnum.Brfalse, "bool flag = false; while(!flag) {DummyMethod();}"},
            new object[] {OpCodeEnum.Brfalse_S, "bool flag = false; while(!flag) {DummyMethod();}"},
            new object[] {OpCodeEnum.Brtrue, "bool flag = true; while(flag) {DummyMethod();}"},
            new object[] {OpCodeEnum.Brtrue_S, "bool flag = true; while(flag) {DummyMethod();}"},
        };

    [Theory]
    [MemberData(nameof(TestData))]
    public void Check_Passes_Branch_Has_Branch_Count(OpCodeEnum opCodeEnum, string infiniteLoopLogic)
    {
	    infiniteLoopLogic =
		    infiniteLoopLogic.Replace("DummyMethod();", "ExecutionObserverProxy.BranchCount(); DummyMethod();");
	    var opCode = OpCodeFixtures.OpCodeLookup[opCodeEnum];
	    const string methodName = "InfiniteLoop";
	    var method = @"
            private void DummyMethod(){
				ExecutionObserverProxy.CallCount();
            }
            public void InfiniteLoop()
            {
				ExecutionObserverProxy.CallCount();
				" + infiniteLoopLogic + @"
            }";
	    var builder = new SourceCodeBuilder("TestContract").AddClass(ObserverProxyClassSource).AddMethod(method);
	    var source = builder.Build();
	    var module = CompileToAssemblyDefinition(source).MainModule;
	    var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(x => x.Name == methodName);
	    methodDefinition.MaybeReplaceShortFormOpCodeWithLongForm(opCode);
	    module.AssertMethodHasOpCode(methodName, opCode);
	    methodDefinition.Body.OptimizeMacros();
	    var errorMessages = new ObserverProxyValidator().Validate(module, new CancellationToken())
		    .Select(r => r.Message).ToList();

	    Assert.Empty(errorMessages);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public void Check_Fails_Branch_Has_No_Branch_Count(OpCodeEnum opCodeEnum, string infiniteLoopLogic)
    {
        var opCode = OpCodeFixtures.OpCodeLookup[opCodeEnum];
        const string methodName = "InfiniteLoop";
        var method = @"
            private void DummyMethod(){
				ExecutionObserverProxy.CallCount();
            }
            public void InfiniteLoop()
            {
				ExecutionObserverProxy.CallCount();
				" + infiniteLoopLogic + @"
            }";
        var builder = new SourceCodeBuilder("TestContract").AddClass(ObserverProxyClassSource).AddMethod(method);
        var source = builder.Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(x => x.Name == methodName);
        methodDefinition.MaybeReplaceShortFormOpCodeWithLongForm(opCode);
        module.AssertMethodHasOpCode(methodName, opCode);
        var errorMessages = new ObserverProxyValidator().Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();
        Assert.Contains(errorMessages,
            s => s == "Missing execution observer branch count call detected. [Contract > InfiniteLoop]");
    }

    [Fact]
    public void Check_Fails_Method_Has_No_Call_Count()
    {
        var method = "private void DummyMethod(){DummyMethod();}";
        var builder = new SourceCodeBuilder("TestContract").AddClass(ObserverProxyClassSource).AddMethod(method);
        var source = builder.Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var errorMessages = new ObserverProxyValidator().Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();

        Assert.Contains(errorMessages,
            s => s == "Missing execution observer call count call detected. [Contract > DummyMethod]");
    }

    [Fact]
    public void Check_Fails_Bad_Proxy_Definition_Bad_BranchCount()
    {
        var badProxyDefinition = @"
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
		// Method body removed
	}

	public static void CallCount ()
	{
		if (_observer != null) {
			_observer.CallCount ();
		}
	}
}";
        var method = @"
            private void DummyMethod(){
				ExecutionObserverProxy.CallCount();
            }";
        var builder = new SourceCodeBuilder("TestContract").AddClass(badProxyDefinition).AddMethod(method);
        var source = builder.Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var errorMessages = new ObserverProxyValidator().Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();
        Assert.Single(errorMessages);

        Assert.Contains("proxy method body is tampered", errorMessages.Single());
    }

    [Fact]
    public void Check_Fails_Bad_Proxy_Definition_Bad_CallCount()
    {
	    var badProxyDefinition = @"
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
		// Method body removed
	}
}";
	    var method = @"
            private void DummyMethod(){
				ExecutionObserverProxy.CallCount();
            }";
	    var builder = new SourceCodeBuilder("TestContract").AddClass(badProxyDefinition).AddMethod(method);
	    var source = builder.Build();
	    var module = CompileToAssemblyDefinition(source).MainModule;
	    var errorMessages = new ObserverProxyValidator().Validate(module, new CancellationToken())
		    .Select(r => r.Message).ToList();
	    Assert.Single(errorMessages);

	    Assert.Contains("proxy method body is tampered", errorMessages.Single());
    }

    [Fact]
    public void Check_Fails_Calling_SetObserver()
    {

	    var method = @"
            private void DummyMethod(){
				ExecutionObserverProxy.CallCount();
				ExecutionObserverProxy.SetObserver(null);
            }";
	    var builder = new SourceCodeBuilder("TestContract").AddClass(ObserverProxyClassSource).AddMethod(method);
	    var source = builder.Build();
	    var module = CompileToAssemblyDefinition(source).MainModule;
	    var errorMessages = new ObserverProxyValidator().Validate(module, new CancellationToken())
		    .Select(r => r.Message).ToList();
	    Assert.Single(errorMessages);

	    Assert.Contains("Proxy initialize call detected from within the contract. [Contract > DummyMethod]", errorMessages.Single());
    }
    #region Private Helpers

    private const string ObserverProxyClassSource = @"
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
}";

    #endregion
}