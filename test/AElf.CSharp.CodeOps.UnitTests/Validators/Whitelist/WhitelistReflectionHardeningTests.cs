using System.Linq;
using System.Threading;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Whitelist;

/// <summary>
/// Covers the whitelist hardening around reflection:
/// - F1: System.Type is denied except GetTypeFromHandle / op_Equality / op_Inequality.
/// - F2: method parameter (and generic-argument) types are validated.
/// - F3: method local-variable types are validated.
/// Plus the compatibility additions (RuntimeTypeHandle / RuntimeFieldHandle) that keep typeof()
/// and hardcoded array initialization passing.
/// </summary>
public class WhitelistReflectionHardeningTests : CSharpCodeOpsTestBase
{
    // F1: typeof(x) (=> Type.GetTypeFromHandle) and Type comparison (=> Type.op_Equality) must pass.
    [Fact]
    public void Allows_Typeof_And_Type_Equality()
    {
        var results = ValidateContractMethod(@"
    public bool Foo()
    {
        return typeof(string) == typeof(int);
    }");
        Assert.DoesNotContain(results, r => r.Info != null && r.Info.ReferencingMethod == "Foo");
    }

    // F2 compatibility: RuntimeFieldHandle is whitelisted, so hardcoded array init (which lowers to
    // RuntimeHelpers.InitializeArray(Array, RuntimeFieldHandle)) keeps passing.
    [Fact]
    public void Allows_Hardcoded_Array_Initialization()
    {
        var results = ValidateContractMethod(@"
    public byte[] Foo()
    {
        return new byte[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24};
    }");
        Assert.DoesNotContain(results, r => r.Info != null && r.Info.ReferencingMethod == "Foo");
    }

    // F1 + F2: string-based reflection dispatch is rejected — Type.GetType/InvokeMember are denied
    // members, and the BindingFlags parameter of InvokeMember is now caught by parameter validation.
    [Fact]
    public void Rejects_Type_Reflection_Dispatch()
    {
        var results = ValidateContractMethod(@"
    public object Foo(byte[] code)
    {
        System.Type t = System.Type.GetType(""System.Reflection.Assembly"");
        var flags = System.Reflection.BindingFlags.Public;
        return t.InvokeMember(""Load"", flags, null, null, new object[]{ code });
    }");
        Assert.Contains(results, r => r.Info != null && r.Info.Type == "Type" && r.Info.Member == "InvokeMember");
        Assert.Contains(results, r => r.Info != null && r.Info.Type == "BindingFlags");
    }

    // F2: a denied type used only as a parameter (never called on) is now caught.
    [Fact]
    public void Rejects_Denied_Parameter_Type()
    {
        var results = ValidateContractMethod(@"
    public string Foo(System.Reflection.Assembly a)
    {
        return a == null ? ""x"" : ""y"";
    }");
        Assert.Contains(results, r => r.Info != null && r.Info.Type.Contains("Assembly") &&
                                      r.Info.ReferencingMethod == "Foo");
    }

    private System.Collections.Generic.List<ValidationResult> ValidateContractMethod(string method)
    {
        var source = new SourceCodeBuilder("TestContract").AddMethod(method).Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var validator = new WhitelistValidator(new WhitelistProvider());
        return validator.Validate(module, new CancellationToken()).ToList();
    }
}
