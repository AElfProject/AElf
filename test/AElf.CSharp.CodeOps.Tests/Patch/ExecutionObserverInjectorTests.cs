using System.Linq;
using AElf.CSharp.CodeOps.Patchers.Module;
using AElf.Runtime.CSharp.Tests.BadContract;
using AElf.Runtime.CSharp.Tests.TestContract;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps.Patch;

public class ExecutionObserverInjectorTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void ConstructCounterProxyTest()
    {
        var module = GetContractModule(typeof(TestContract));
        var ns = "AElf.CSharp.CodeOps";
        var typeDefinition = new Patchers.Module.CallAndBranchCounts.Patch(module, ns).ObserverType;
        typeDefinition.Methods.Count.ShouldBe(3);
        typeDefinition.Methods.Select(method => method.DeclaringType.FullName)
            .ShouldAllBe(name => name == string.Concat(ns, ".", nameof(ExecutionObserverProxy)));
        new[]
        {
            nameof(ExecutionObserverProxy.SetObserver), nameof(ExecutionObserverProxy.BranchCount),
            nameof(ExecutionObserverProxy.CallCount)
        }.ShouldAllBe(name => typeDefinition.Methods.Select(method => method.Name).Contains(name));
    }

    [Fact]
    public void ExecutionObserverInjectorPatchTest()
    {
        var module = GetContractModule(typeof(TestContract));

        var typeDefinition = new Patchers.Module.CallAndBranchCounts.Patch(module, "AElf.Test").ObserverType;

        var executionObserverInjector = new Patchers.Module.CallAndBranchCounts.Patcher();
        executionObserverInjector.Patch(module);
        var typePatched = module.Types.Single(t => t.Name == nameof(ExecutionObserverProxy));
        typePatched.HasSameFields(typeDefinition).ShouldBeTrue();

        foreach (var method in typePatched.Methods)
            method.HasSameBody(typeDefinition.Methods.Single(m => m.Name == method.Name)).ShouldBeTrue();
    }

    [Fact]
    public void ExecutionObserverInjectorPatchTest2()
    {
        var module = GetContractModule(typeof(BadContract));

        var executionObserverInjector = new Patchers.Module.CallAndBranchCounts.Patcher();
        executionObserverInjector.Patch(module);
    }
}