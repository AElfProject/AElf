using AElf.CSharp.CodeOps.Instructions;
using AElf.CSharp.CodeOps.Patchers.Module;
using AElf.Kernel.CodeCheck;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Patchers.Module.SaftState;

public class PatcherTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void Not_Patched_ReadonlyState_Primitives()
    {
        var states = @"
        public ReadonlyState<bool> ReadonlyBool { get; set; }
        public ReadonlyState<int> ReadonlyInt { get; set; }
        public ReadonlyState<uint> ReadonlyUInt { get; set; }
        public ReadonlyState<long> ReadonlyLong { get; set; }
        public ReadonlyState<ulong> ReadonlyULong { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.ReadonlyBool.Value = true;
        State.ReadonlyInt.Value = 42;
        State.ReadonlyUInt.Value = 42u;
        State.ReadonlyLong.Value = 42L;
        State.ReadonlyULong.Value = 42uL;
    }";

        var expectedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		base.State.ReadonlyBool.Value = true;
        base.State.ReadonlyInt.Value = 42;
        base.State.ReadonlyUInt.Value = 42u;
        base.State.ReadonlyLong.Value = 42L;
        base.State.ReadonlyULong.Value = 42uL;
	}
}
";
        DoTest(states, method, expectedContractCode);
    }

    [Fact]
    public void Not_Patched_SingletonState_Primitives()
    {
        var states = @"
        public SingletonState<bool> SingletonBool { get; set; }
        public SingletonState<int> SingletonInt { get; set; }
        public SingletonState<uint> SingletonUInt { get; set; }
        public SingletonState<long> SingletonLong { get; set; }
        public SingletonState<ulong> SingletonULong { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.SingletonBool.Value = true;
        State.SingletonInt.Value = 42;
        State.SingletonUInt.Value = 42u;
        State.SingletonLong.Value = 42L;
        State.SingletonULong.Value = 42uL;
    }";

        var expectedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		base.State.SingletonBool.Value = true;
        base.State.SingletonInt.Value = 42;
        base.State.SingletonUInt.Value = 42u;
        base.State.SingletonLong.Value = 42L;
        base.State.SingletonULong.Value = 42uL;
	}
}
";
        DoTest(states, method, expectedContractCode);
    }

    [Fact]
    public void Not_Patched_MappedState_Primitives()
    {
        var states = @"
        public MappedState<int,bool> MappedBool { get; set; }
        public MappedState<int,int> MappedInt { get; set; }
        public MappedState<int,uint> MappedUInt { get; set; }
        public MappedState<int,long> MappedLong { get; set; }
        public MappedState<int,ulong> MappedULong { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.MappedBool[1] = true;
        State.MappedInt[1] = 42;
        State.MappedUInt[1] = 42u;
        State.MappedLong[1] = 42L;
        State.MappedULong[1] = 42uL;
    }";

        var expectedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		base.State.MappedBool [1] = true;
        base.State.MappedInt [1] = 42;
        base.State.MappedUInt [1] = 42u;
        base.State.MappedLong [1] = 42L;
        base.State.MappedULong [1] = 42uL;
	}
}
";
        DoTest(states, method, expectedContractCode);
    }

    [Fact]
    public void Patched_ReadonlyState()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.ReadonlyFoo.Value = new StringValue(){
            Value = ""abc""
        };
    }";

        var expectedContractCode = @"using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		ReadonlyState<StringValue> readonlyFoo = base.State.ReadonlyFoo;
		object obj = new StringValue {
			Value = ""abc""
        };
        readonlyFoo.Value = (StringValue)base.Context.ValidateStateSize (obj);
    }
}
";
        DoTest(states, method, expectedContractCode);
    }


    [Fact]
    public void Patched_SingletonState()
    {
        var states = @"
        public SingletonState<StringValue> SingletonFoo { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.SingletonFoo.Value = new StringValue(){
            Value = ""abc""
        };
    }";

        var expectedContractCode = @"using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		SingletonState<StringValue> singletonFoo = base.State.SingletonFoo;
		object obj = new StringValue {
			Value = ""abc""
        };
        singletonFoo.Value = (StringValue)base.Context.ValidateStateSize (obj);
    }
}
";
        DoTest(states, method, expectedContractCode);
    }


    [Fact]
    public void Patched_MappedState_One_Key()
    {
        var states = @"
        public MappedState<int, StringValue> MappedFoo { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.MappedFoo[1] = new StringValue(){
            Value = ""abc""
        };
    }";

        var expectedContractCode = @"using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedFoo = base.State.MappedFoo;
		object obj = new StringValue {
			Value = ""abc""
        };
        mappedFoo [1] = (StringValue)base.Context.ValidateStateSize (obj);
    }
}
";
        DoTest(states, method, expectedContractCode);
    }

    [Fact]
    public void Patched_MappedState_Two_Keys()
    {
        var states = @"
        public MappedState<int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.MappedFoo[1][2] = new StringValue(){
            Value = ""abc""
        };
    }";

        var expectedContractCode = @"using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedState = base.State.MappedFoo [1];
		object obj = new StringValue {
			Value = ""abc""
        };
        mappedState [2] = (StringValue)base.Context.ValidateStateSize (obj);
    }
}
";
        DoTest(states, method, expectedContractCode);
    }

    [Fact]
    public void Patched_MappedState_Three_Keys()
    {
        var states = @"
        public MappedState<int, int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.MappedFoo[1][2][3] = new StringValue(){
            Value = ""abc""
        };
    }";

        var expectedContractCode = @"using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedState = base.State.MappedFoo [1] [2];
		object obj = new StringValue {
			Value = ""abc""
        };
        mappedState [3] = (StringValue)base.Context.ValidateStateSize (obj);
    }
}
";
        DoTest(states, method, expectedContractCode);
    }

    [Fact]
    public void Patched_MappedState_Four_Keys()
    {
        var states = @"
        public MappedState<int, int, int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.MappedFoo[1][2][3][4] = new StringValue(){
            Value = ""abc""
        };
    }";

        var expectedContractCode = @"using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedState = base.State.MappedFoo [1] [2] [3];
		object obj = new StringValue {
			Value = ""abc""
        };
        mappedState [4] = (StringValue)base.Context.ValidateStateSize (obj);
    }
}
";
        DoTest(states, method, expectedContractCode);
    }

    [Fact]
    public void Patched_ReadonlyState_And_SingletonState_And_MappedState()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
        public SingletonState<StringValue> SingletonFoo { get; set; }
        public MappedState<int, int, int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
    public void Update(int i)
    {
        State.ReadonlyFoo.Value = new StringValue(){
            Value = ""abc""
        };
        State.SingletonFoo.Value = new StringValue(){
            Value = ""abc""
        };
        State.MappedFoo[1][2][3][4] = new StringValue(){
            Value = ""abc""
        };
    }";

        var expectedContractCode = @"using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace TestContract;

public class Contract : Container.ContractBase
{
	public void Update (int i)
	{
		ReadonlyState<StringValue> readonlyFoo = base.State.ReadonlyFoo;
		object obj = new StringValue {
			Value = ""abc""
		};
		readonlyFoo.Value = (StringValue)base.Context.ValidateStateSize (obj);

		SingletonState<StringValue> singletonFoo = base.State.SingletonFoo;
		object obj2 = new StringValue {
			Value = ""abc""
		};
		singletonFoo.Value = (StringValue)base.Context.ValidateStateSize (obj2);

		MappedState<int, StringValue> mappedState = base.State.MappedFoo [1] [2] [3];
		object obj3 = new StringValue {
			Value = ""abc""
		};
		mappedState [4] = (StringValue)base.Context.ValidateStateSize (obj3);
	}
}
";
        DoTest(states, method, expectedContractCode);
    }

    [Fact(Skip = "Throw exception temporarily disabled https://github.com/AElfProject/AElf/issues/3389")]
    public void Patcher_Throws_Invalid_Code_In_Separate_Class()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
        public SingletonState<StringValue> SingletonFoo { get; set; }
        public MappedState<int, int, int, int, StringValue> MappedFoo { get; set; }
";

        var classSource = @"
public class AnotherClass {
    public void Update(StateType state)
    {
        state.ReadonlyFoo.Value = new StringValue(){
            Value = ""abc""
        };
        state.SingletonFoo.Value = new StringValue(){
            Value = ""abc""
        };
        state.MappedFoo[1][2][3][4] = new StringValue(){
            Value = ""abc""
        };
    }
}
";
        var builder = new SourceCodeBuilder("TestContract")
            .AddStateField(states)
            .AddClass(classSource);
        var asm = CompileToAssemblyDefinition(builder.Build());
        var module = asm.MainModule;
        Assert.Throws<InvalidCodeException>(()=>ApplyPatch(module));
    }

    
    [Fact(Skip = "Throw exception temporarily disabled https://github.com/AElfProject/AElf/issues/3389")]
    public void Patcher_Throws_Invalid_Code_In_Separate_Class_Static_Method()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
        public SingletonState<StringValue> SingletonFoo { get; set; }
        public MappedState<int, int, int, int, StringValue> MappedFoo { get; set; }
";

        var classSource = @"
public class AnotherClass {
    public static void Update(StateType state)
    {
        state.ReadonlyFoo.Value = new StringValue(){
            Value = ""abc""
        };
        state.SingletonFoo.Value = new StringValue(){
            Value = ""abc""
        };
        state.MappedFoo[1][2][3][4] = new StringValue(){
            Value = ""abc""
        };
    }
}
";

        var builder = new SourceCodeBuilder("TestContract")
            .AddStateField(states)
            .AddClass(classSource);
        var asm = CompileToAssemblyDefinition(builder.Build());
        var module = asm.MainModule;
        Assert.Throws<InvalidCodeException>(()=>ApplyPatch(module));
    }
    
    [Fact(Skip = "Throw exception temporarily disabled https://github.com/AElfProject/AElf/issues/3389")]
    public void Patcher_Throws_Invalid_Code_In_Separate_Class_Extension_Method()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
        public SingletonState<StringValue> SingletonFoo { get; set; }
        public MappedState<int, int, int, int, StringValue> MappedFoo { get; set; }
";

        var classSource = @"
public static class AnotherClass {
    public static void Update(this StateType state)
    {
        state.ReadonlyFoo.Value = new StringValue(){
            Value = ""abc""
        };
        state.SingletonFoo.Value = new StringValue(){
            Value = ""abc""
        };
        state.MappedFoo[1][2][3][4] = new StringValue(){
            Value = ""abc""
        };
    }
}
";

        var builder = new SourceCodeBuilder("TestContract")
            .AddStateField(states)
            .AddClass(classSource);
        var asm = CompileToAssemblyDefinition(builder.Build());
        var module = asm.MainModule;
        Assert.Throws<InvalidCodeException>(()=>ApplyPatch(module));
    }
    
    [Fact(Skip = "Throw exception temporarily disabled https://github.com/AElfProject/AElf/issues/3389")]
    public void Patcher_Throws_Invalid_Code_In_Nested_Class()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
        public SingletonState<StringValue> SingletonFoo { get; set; }
        public MappedState<int, int, int, int, StringValue> MappedFoo { get; set; }
";

        var classSource = @"
public class OuterClass{
public class AnotherClass {
    public void Update(StateType state)
    {
        state.ReadonlyFoo.Value = new StringValue(){
            Value = ""abc""
        };
        state.SingletonFoo.Value = new StringValue(){
            Value = ""abc""
        };
        state.MappedFoo[1][2][3][4] = new StringValue(){
            Value = ""abc""
        };
    }
}
}
";

        var builder = new SourceCodeBuilder("TestContract")
            .AddStateField(states)
            .AddClass(classSource);
        var asm = CompileToAssemblyDefinition(builder.Build());
        var module = asm.MainModule;
        Assert.Throws<InvalidCodeException>(()=>ApplyPatch(module));
    }
    #region Private Helpers

    private static ModuleDefinition ApplyPatch(ModuleDefinition module)
    {
        var instructionInjector = new StateWrittenInstructionInjector();
        var patcher = new StateWrittenSizeLimitMethodInjector(instructionInjector);
        patcher.Patch(module);
        return module;
    }

    private void DoTest(string states, string method, string expectedContractCode)
    {
        var builder = new SourceCodeBuilder("TestContract")
            .AddStateField(states)
            .AddMethod(method);
        var asm = CompileToAssemblyDefinition(builder.Build());
        var module = asm.MainModule;
        ApplyPatch(module);
        var patchedCode =
            DecompileType(module.GetAllTypes().Single(t => t.FullName == builder.ContractTypeFullName));
        Assert.Equal(expectedContractCode.CleanCode(), patchedCode.CleanCode());
    }

    #endregion
}