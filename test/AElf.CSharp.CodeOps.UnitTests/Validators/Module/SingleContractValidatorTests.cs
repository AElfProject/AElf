using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.CSharp.CodeOps.Validators.Module;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Assembly;

public class SingleContractValidatorTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void Check_Passes_If_One_Contract_Is_Found()
    {
        var source = @"
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TestContractNamespace
{
    public class TestContractState : ContractState
    {
    }

    public class Container
    {
        public class ContractBase : AElf.Sdk.CSharp.CSharpSmartContract<TestContractState>
        {
        }
    }
    public class TestContract : Container.ContractBase
    {
    }
}
";
        var module = CompileToAssemblyDefinition(source).MainModule;
        var errorMessages = new SingleContractValidator().Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();
        Assert.Empty(errorMessages);
    }

    [Fact]
    public void Check_Fails_If_No_Contract_Is_Found()
    {
        var source = @"
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TestContractNamespace
{
    public class TestContractState : ContractState
    {
    }

    public class Container
    {
        public class ContractBase : AElf.Sdk.CSharp.CSharpSmartContract<TestContractState>
        {
        }
    }
    // public class TestContract : Container.ContractBase
    // {
    // }
}
";
        var module = CompileToAssemblyDefinition(source).MainModule;
        var errorMessages = new SingleContractValidator().Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();

        Assert.Single(errorMessages);
        Assert.Contains("Contract implementation is not found in the assembly.", errorMessages.Single());
    }

    [Fact]
    public void Check_Fails_If_More_Than_One_Contracts_Are_Found()
    {
        var source = @"
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace TestContractNamespace
{
    public class TestContractState : ContractState
    {
    }

    public class Container
    {
        public class ContractBase : AElf.Sdk.CSharp.CSharpSmartContract<TestContractState>
        {
        }
    }
    public class TestContract : Container.ContractBase
    {
    }
    public class AnotherTestContract : Container.ContractBase
    {
    }
}
";
        var module = CompileToAssemblyDefinition(source).MainModule;
        var errorMessages = new SingleContractValidator().Validate(module, new CancellationToken())
            .Select(r => r.Message).ToList();

        Assert.Single(errorMessages);
        Assert.Contains("Only one contract implementation is allowed in the assembly.", errorMessages.Single());
    }
}