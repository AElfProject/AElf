using AElf.CSharp.CodeOps.Instructions;
using AElf.CSharp.CodeOps.Validators.Method;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Module.SafeState;

public class InstructionInjectionValidatorTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void Check_ReadonlyState_Passes_If_Has_ValidateStateSize_Call()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		ReadonlyState<StringValue> readonlyFoo = base.State.ReadonlyFoo;
		var obj = new StringValue {
			Value = ""abc""
        };
        readonlyFoo.Value = (StringValue)base.Context.ValidateStateSize (obj);
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public void Check_ReadonlyState_Fails_If_No_ValidateStateSize_Call()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		ReadonlyState<StringValue> readonlyFoo = base.State.ReadonlyFoo;
		var obj = new StringValue {
			Value = ""abc""
        };
        readonlyFoo.Value = obj;
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Single(errorMessages);
        Assert.Equal("AElf.CSharp.CodeOps.Instructions.StateWrittenInstructionInjector validation failed.",
            errorMessages.Single());
    }

    [Fact]
    public void Check_SingletonState_Passes_If_Has_ValidateStateSize_Call()
    {
        var states = @"
        public SingletonState<StringValue> SingletonFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		SingletonState<StringValue> singletonFoo = base.State.SingletonFoo;
		var obj = new StringValue {
			Value = ""abc""
        };
        singletonFoo.Value = (StringValue)base.Context.ValidateStateSize (obj);
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public void Check_SingletonState_Fails_If_No_ValidateStateSize_Call()
    {
        var states = @"
        public SingletonState<StringValue> SingletonFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		SingletonState<StringValue> singletonFoo = base.State.SingletonFoo;
		var obj = new StringValue {
			Value = ""abc""
        };
        singletonFoo.Value = obj;
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Single(errorMessages);
        Assert.Equal("AElf.CSharp.CodeOps.Instructions.StateWrittenInstructionInjector validation failed.",
            errorMessages.Single());
    }

    [Fact]
    public void Check_MappedState_One_Key_Passes_If_Has_ValidateStateSize_Call()
    {
        var states = @"
        public MappedState<int, StringValue> MappedFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedFoo = base.State.MappedFoo;
		object obj = new StringValue {
			Value = ""abc""
        };
        mappedFoo [1] = (StringValue)base.Context.ValidateStateSize (obj);
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public void Check_MappedState_One_Key_Fails_If_No_ValidateStateSize_Call()
    {
        var states = @"
        public MappedState<int, StringValue> MappedFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedFoo = base.State.MappedFoo;
		var obj = new StringValue {
			Value = ""abc""
        };
        mappedFoo [1] = obj;
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Single(errorMessages);
        Assert.Equal("AElf.CSharp.CodeOps.Instructions.StateWrittenInstructionInjector validation failed.",
            errorMessages.Single());
    }

    [Fact]
    public void Check_MappedState_Two_Keys_Passes_If_Has_ValidateStateSize_Call()
    {
        var states = @"
        public MappedState<int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedFoo = base.State.MappedFoo[1];
		var obj = new StringValue {
			Value = ""abc""
        };
        mappedFoo [2] = (StringValue)base.Context.ValidateStateSize (obj);
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public void Check_MappedState_Two_Keys_Fails_If_No_ValidateStateSize_Call()
    {
        var states = @"
        public MappedState<int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedFoo = base.State.MappedFoo[1];
		var obj = new StringValue {
			Value = ""abc""
        };
        mappedFoo [2] = obj;
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Single(errorMessages);
        Assert.Equal("AElf.CSharp.CodeOps.Instructions.StateWrittenInstructionInjector validation failed.",
            errorMessages.Single());
    }

    [Fact]
    public void Check_MappedState_Three_Keys_Passes_If_Has_ValidateStateSize_Call()
    {
        var states = @"
        public MappedState<int, int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedFoo = base.State.MappedFoo[1][2];
		var obj = new StringValue {
			Value = ""abc""
        };
        mappedFoo [3] = (StringValue)base.Context.ValidateStateSize (obj);
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public void Check_MappedState_Three_Keys_Fails_If_No_ValidateStateSize_Call()
    {
        var states = @"
        public MappedState<int, int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
	public void Update (int i)
	{
		MappedState<int, StringValue> mappedFoo = base.State.MappedFoo[1][2];
		var obj = new StringValue {
			Value = ""abc""
        };
        mappedFoo [3] = obj;
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Single(errorMessages);
        Assert.Equal("AElf.CSharp.CodeOps.Instructions.StateWrittenInstructionInjector validation failed.",
            errorMessages.Single());
    }

    [Fact]
    public void Check_ReadonlyState_And_SingletonState_And_MappedState_Passes_If_Has_ValidateStateSize_Call()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
        public SingletonState<StringValue> SingletonFoo { get; set; }
        public MappedState<int, int, int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
    public void Update(int i)
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
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public void Check_ReadonlyState_And_SingletonState_And_MappedState_Fails_If_No_ValidateStateSize_Call()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
        public SingletonState<StringValue> SingletonFoo { get; set; }
        public MappedState<int, int, int, int, StringValue> MappedFoo { get; set; }
";

        var method = @"
    public void Update(int i)
    {
		ReadonlyState<StringValue> readonlyFoo = base.State.ReadonlyFoo;
		var obj = new StringValue {
			Value = ""abc""
		};
		readonlyFoo.Value = obj;

		SingletonState<StringValue> singletonFoo = base.State.SingletonFoo;
		var obj2 = new StringValue {
			Value = ""abc""
		};
		singletonFoo.Value = obj2;

		MappedState<int, StringValue> mappedState = base.State.MappedFoo [1] [2] [3];
		var obj3 = new StringValue {
			Value = ""abc""
		};
		mappedState [4] = obj3;
    }";
        var errorMessages = PrepareAndRunValidation(states, method);
        Assert.Equal(3, errorMessages.Count);
        Assert.Equal("AElf.CSharp.CodeOps.Instructions.StateWrittenInstructionInjector validation failed.",
            errorMessages.Distinct().Single());
    }

    [Fact]
    public void
        Check_State_Update_Not_Allowed_In_Non_Contract_Class()
    {
        var states = @"
        public ReadonlyState<StringValue> ReadonlyFoo { get; set; }
";

        var classSource = @"
public class AnotherClass {
	public void Update (StateType state)
	{
		ReadonlyState<StringValue> readonlyFoo = state.ReadonlyFoo;
		var obj = new StringValue {
			Value = ""abc""
		};
		readonlyFoo.Value = obj;
	}
}
";
        var errorMessages = PrepareAndRunValidation(states, null, classSource);
        Assert.Single(errorMessages);
        Assert.Equal(
            "AElf.CSharp.CodeOps.Instructions.StateWrittenInstructionInjector validation failed. Updating state in non-contract class is not allowed.",
            errorMessages.Single()
        );
    }

    #region Private Helpers

    private List<string> PrepareAndRunValidation(string states, string? method = null, string? classSource = null)
    {
        var builder = new SourceCodeBuilder("TestContract")
            .AddStateField(states);
        if (method != null)
        {
            builder = builder.AddMethod(method);
        }

        if (classSource != null)
        {
            builder = builder.AddClass(classSource);
        }

        var asm = CompileToAssemblyDefinition(builder.Build());
        var module = asm.MainModule;
        var instructionInjector = new StateWrittenInstructionInjector();
        return new InstructionInjectionValidator(instructionInjector)
            .Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();
    }

    #endregion
}