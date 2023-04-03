using AElf.CSharp.CodeOps.Validators.Module;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Module.StaticFields;

public class NoStaticFieldsValidatorTests : CSharpCodeOpsTestBase
{

    [Fact]
    public void Check_Fails_If_Contract_Has_A_Static_Field()
    {
        var states = "public static int StaticField;";
        var errorMessages = PrepareAndRunValidation(states);
        Assert.Single(errorMessages);
        Assert.Contains("Has static field", errorMessages.Single());
    }

    [Fact]
    public void Check_Fails_If_Contract_Has_A_Static_Property()
    {
        var states = "public static int StaticField { get; set; }";
        var errorMessages = PrepareAndRunValidation(states);
        Assert.Single(errorMessages);
        Assert.Contains("Has static field", errorMessages.Single());
    }

    [Fact]
    public void Check_Fails_If_A_Nested_Class_Has_A_Static_Field()
    {
        var source = @"
class NestedClass {
        public static int StaticField;
}
";
        var errorMessages = PrepareAndRunValidation(null, null, classSource: source, isNested: true);
        Assert.Single(errorMessages);
        Assert.Contains("Has static field", errorMessages.Single());
    }

    [Fact]
    public void Check_Fails_If_A_Nested_Class_Has_A_Static_Property()
    {
        var source = @"
class NestedClass {
        public static int StaticField { get; set; }
}
";
        var errorMessages = PrepareAndRunValidation(null, null, classSource: source, isNested: true);
        Assert.Single(errorMessages);
        Assert.Contains("Has static field", errorMessages.Single());
    }

    [Fact]
    public void Check_Fails_If_A_Separate_Nested_Class_Has_A_Static_Field()
    {
        var source = @"
class SeparateClass{
class NestedClass {
        public static int StaticField;
}
}
";
        var errorMessages = PrepareAndRunValidation(null, null, classSource: source);
        Assert.Single(errorMessages);
        Assert.Contains("Has static field", errorMessages.Single());
    }

    [Fact]
    public void Check_Fails_If_A_Separate_Nested_Class_Has_A_Static_Property()
    {
        var source = @"
class SeparateClass{
class NestedClass {
        public static int StaticField { get; set; }
}
}
";
        var errorMessages = PrepareAndRunValidation(null, null, classSource: source);
        Assert.Single(errorMessages);
        Assert.Contains("Has static field", errorMessages.Single());
    }

    #region Private Helpers

    private List<string> PrepareAndRunValidation(string? states, string? method = null, string? classSource = null,
        bool isNested = false)
    {
        var builder = new SourceCodeBuilder("TestContract");
        if (states != null)
        {
            builder.AddStateField(states);
        }

        if (method != null)
        {
            builder = builder.AddMethod(method);
        }

        if (classSource != null)
        {
            builder = builder.AddClass(classSource, isNested);
        }

        var asm = CompileToAssemblyDefinition(builder.Build());
        var module = asm.MainModule;
        return new NoStaticFieldsValidator().Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();
    }

    #endregion
}