using AElf.CSharp.CodeOps.Validators.Whitelist;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Module.SafeMethods;

public class StringMethodsValidatorTests : CSharpCodeOpsTestBase
{
    [Theory]
    [InlineData("public string Foo (string a, string b) => a + b;")]
    [InlineData("public string Foo (string a, string b, string c) => a + b + c;")]
    [InlineData("public string Foo (string a, string b, string c, string d) => a + b + c + d;")]
    [InlineData("public string Foo (string a, string b, string c, string d) => string.Concat(new string[]{a, b, c, d});")]
    [InlineData("public string Foo (string a, string b, string c, string d) => string.Concat(new object[]{a, b, c, d});")]
    public void Check_Fails_If_Using_String_Concat(string method)
    {
        var builder = new SourceCodeBuilder("TestContract").AddMethod(method);
        var source = builder.Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var whitelistProvider = new WhitelistProvider();
        var errorMessages =  new WhitelistValidator(whitelistProvider).Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();
        Assert.Contains("Concat in System.String is not allowed.", errorMessages.Single());
    }
}