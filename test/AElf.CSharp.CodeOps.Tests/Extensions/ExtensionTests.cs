using System;
using System.Linq;
using System.Reflection;
using AElf.Runtime.CSharp.Tests.TestContract;
using AElf.Sdk.CSharp.State;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps.Extensions;

public class ExtensionTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void HasSameBodyTests()
    {
        var contractModule = GetModule(typeof(TestContract));
        var sdkModule = GetModule(typeof(StructuredState));
        var type1 = sdkModule.Types.First(t => t.Name == nameof(StructuredState));
        var type2 = contractModule.Types.First(t => t.Name == nameof(TestContractState)).BaseType.Resolve()
            .BaseType.Resolve();

        // has same field 
        type1.HasSameFields(type2).ShouldBeTrue();

        for (var i = 0; i < type1.Methods.Count; i++)
        {
            type1.Methods[i].HasSameBody(type1.Methods[i]).ShouldBeTrue();
            type1.Methods[i].HasSameParameters(type1.Methods[i]).ShouldBeTrue();
        }

        var stateType = contractModule.Types.First(t => t.Name == nameof(TestContractState));
        stateType.IsStateImplementation().ShouldBeTrue();

        var contractType = contractModule.Types.First(t => t.Name == nameof(TestContract));
        contractType.IsContractImplementation().ShouldBeTrue();
    }

    [Fact(Skip = "Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388")]
    public void GetFieldsTests()
    {
        var contractModule = GetModule(typeof(TestContract));
        var type = contractModule.Types.First(t => t.Name == nameof(TestContract));
        var staticFields = type.GetResetableStaticFields();
        staticFields.Count().ShouldBe(2);
        staticFields.ShouldContain(f => f.DeclaringType.Name == nameof(TestContract) && f.Name == "i");
        staticFields.ShouldContain(f =>
            f.DeclaringType.Name.Contains("TestNestClass") && f.Name == "k");

        var contractType = contractModule.Types.First(t => t.Name == nameof(TestContract));
        var fields = contractType.GetAllFields(f => true);
        fields.Count().ShouldBe(1);
        fields.ShouldContain(f => f.Name == "i");
    }

    [Fact]
    public void FindContractTests()
    {
        {
            var code = ReadCode(Assembly.GetAssembly(typeof(StructuredState)).Location);
            var asm = Assembly.Load(code);
            Should.Throw<NullReferenceException>(() => asm.FindContractContainer());
            asm.FindContractBaseType().ShouldBeNull();
            Should.Throw<InvalidOperationException>(() => asm.FindContractType().ShouldBeNull());
            asm.FindExecutionObserverProxyType().ShouldBeNull();
        }

        {
            var code = ReadContractCode(typeof(TestContract));
            var asm = Assembly.Load(code);
            asm.FindContractContainer().ShouldNotBeNull();
            asm.FindContractBaseType().ShouldNotBeNull();
            asm.FindContractType().ShouldNotBeNull();
            asm.FindExecutionObserverProxyType().ShouldBeNull();
        }

        {
            var code = ReadPatchedContractCode(typeof(TestContract));
            var asm = Assembly.Load(code);
            asm.FindContractContainer().ShouldNotBeNull();
            asm.FindContractBaseType().ShouldNotBeNull();
            asm.FindContractType().ShouldNotBeNull();
            asm.FindExecutionObserverProxyType().ShouldNotBeNull();
        }
    }
}