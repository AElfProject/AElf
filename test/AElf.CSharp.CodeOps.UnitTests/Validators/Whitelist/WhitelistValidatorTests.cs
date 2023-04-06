using Mono.Cecil;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public class WhitelistValidatorTests
{
    [Fact]
    public void SystemContractIgnoredTest()
    {
        var validator = new WhitelistValidator(new WhitelistProvider());
        Assert.True(validator.SystemContactIgnored);
    }
}

public class SystemContractWhitelistValidatorTests
{
    [Fact]
    public void SystemContractNotIgnoredTest()
    {
        var validator = new SystemContractWhitelistValidator(new SystemContractWhitelistProvider());
        Assert.False(validator.SystemContactIgnored);
    }
}

public class WhitelistValidatorBaseTests
{
    [Fact]
    public void ValidateModuleTest()
    {
        string assemblyCode = @"
using System;

namespace TestAssembly
{
    public class TestType
    {
    }
}";
        var assemblyName = "TestAssembly";
        var tempAssemblyPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        var syntaxTree = CSharpSyntaxTree.ParseText(assemblyCode);
        var compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        using (var fileStream = File.Create(tempAssemblyPath))
        {
            var result = compilation.Emit(fileStream);
            Assert.True(result.Success);
        }

        try
        {
            var assembly = AssemblyDefinition.ReadAssembly(tempAssemblyPath);
            var module = assembly.MainModule;
            var validator = new WhitelistValidator(new WhitelistProvider());
            var results = validator.Validate(module, CancellationToken.None);

            Assert.NotNull(results);
            Assert.Empty(results);
        }
        finally
        {
            File.Delete(tempAssemblyPath);
        }
    }


    [Fact]
    public void ValidateModule_InvalidDependencyTest()
    {
        var assembly =
            AssemblyDefinition.ReadAssembly(typeof(SampleContracts.InvalidDependencyContract).Assembly.Location);
        var module = assembly.MainModule;
        var validator = new WhitelistValidator(new WhitelistProvider());
        var results = validator.Validate(module, CancellationToken.None);
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Message.Contains("Assembly") && r.Message.Contains("is not allowed."));
    }

    [Fact]
    public void ValidateModule_NonWhitelistedTypeTest()
    {
        var assembly =
            AssemblyDefinition.ReadAssembly(typeof(SampleContracts.NonWhitelistedTypeContract).Assembly.Location);
        var module = assembly.MainModule;
        var validator = new WhitelistValidator(new WhitelistProvider());
        var results = validator.Validate(module, CancellationToken.None);

        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Message.Contains("in") && r.Message.Contains("is not allowed."));
    }

    [Fact]
    public void ValidateModule_NonWhitelistedMemberTest()
    {
        var assembly =
            AssemblyDefinition.ReadAssembly(typeof(SampleContracts.NonWhitelistedMemberContract).Assembly.Location);
        var module = assembly.MainModule;
        var validator = new WhitelistValidator(new WhitelistProvider());
        var results = validator.Validate(module, CancellationToken.None);

        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Message.Contains("in") && r.Message.Contains("is not allowed."));
    }
}