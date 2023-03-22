using AElf.CSharp.CodeOps.Validators.Method;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Module.SafeMath;

public class UncheckedMathValidatorTests : CSharpCodeOpsTestBase
{
    [Theory]
    [InlineData("return i + j;")]
    [InlineData("return i - j;")]
    [InlineData("return i * j;")]
    public void Check_Fails_Non_Overflow_Protected_OpCode(string methodBody)
    {
        var errorMessages = PrepareAndRunValidation(methodBody);
        Assert.Contains("contains unsafe OpCode", errorMessages.Single());
    }

    [Theory]
    [InlineData("return i + j;")]
    [InlineData("return i - j;")]
    [InlineData("return i * j;")]
    public void Check_Passes_Overflow_Protected_OpCode(string methodBody)
    {
        var errorMessages = PrepareAndRunValidation($"checked{{ {methodBody} }}");
        Assert.Empty(errorMessages);
    }


    #region Private Helpers

    private List<string> PrepareAndRunValidation(string methodBody)
    {
        var method = @"
	public int Foo (int i, int j)
	{
        " + methodBody + @"
    }";
        var builder = new SourceCodeBuilder("TestContract").AddMethod(method);
        var source = builder.Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(m => m.Name == "Foo");
        return new UncheckedMathValidator().Validate(methodDefinition, new CancellationToken())
            .Select(r => r.Message).ToList();
    }

    #endregion
}