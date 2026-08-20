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

    // F2: a denied type used only as an own-method parameter (never called on) is now caught.
    // NOTE: Binder has no op_Equality, and `is null` emits no call instruction — so unlike an
    // `Assembly a` / `a == null` probe (which Roslyn lowers to Assembly.op_Equality), this test
    // can ONLY pass because own-signature parameter types are scanned.
    [Fact]
    public void Rejects_Denied_Parameter_Type()
    {
        var results = ValidateContractMethod(@"
    public string Foo(System.Reflection.Binder b)
    {
        return b is null ? ""x"" : ""y"";
    }");
        Assert.Contains(results, r => r.Info != null && r.Info.Type.Contains("Binder") &&
                                      r.Info.ReferencingMethod == "Foo");
    }

    // F2: same for a denied type used only as an own-method return type.
    [Fact]
    public void Rejects_Denied_Return_Type()
    {
        var results = ValidateContractMethod(@"
    public System.Reflection.Binder Foo()
    {
        return null;
    }");
        Assert.Contains(results, r => r.Info != null && r.Info.Type.Contains("Binder") &&
                                      r.Info.ReferencingMethod == "Foo");
    }

    // Depth: the whitelist must see types nested deeper than one level. A reflection-dispatch
    // payload in a depth-2 nested type previously passed the whitelist entirely (the dedicated
    // ReflectionValidator did not recognize IReflect either), re-opening the runtime assembly
    // load bypass.
    [Fact]
    public void Rejects_Reflection_Dispatch_In_Deeply_Nested_Type()
    {
        var source = new SourceCodeBuilder("TestContract")
            .AddClass(@"
    public class Outer
    {
        public class Inner
        {
            public static object Pwn(byte[] code)
            {
                System.Type t = typeof(System.Reflection.Assembly);
                System.Reflection.IReflect r = (System.Reflection.IReflect)t;
                return r.InvokeMember(""Load"",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.InvokeMethod,
                    null, null, new object[] { code }, null, null, null);
            }
        }
    }", isNestedInContract: true)
            .Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var results = new WhitelistValidator(new WhitelistProvider())
            .Validate(module, new CancellationToken()).ToList();
        Assert.Contains(results, r => r.Info != null && r.Info.Type == "IReflect");
        Assert.Contains(results, r => r.Info != null && r.Info.Type == "Assembly");
    }

    // Generics: a fully-trusted generic container must not launder a denied type argument.
    [Fact]
    public void Rejects_Generic_Container_Hiding_Denied_Argument()
    {
        var results = ValidateContractMethod(@"
    public int Foo()
    {
        var list = new Google.Protobuf.Collections.RepeatedField<System.Reflection.Assembly>();
        return list.Count;
    }");
        Assert.Contains(results, r => r.Info != null && r.Info.Type.Contains("Assembly") &&
                                      r.Info.ReferencingMethod == "Foo");
    }

    // Compatibility: lambdas / LINQ lower to compiler-generated delegate constructors with an
    // (object, IntPtr) signature. Those ctor parameter types are exempt from scanning, so
    // ordinary lambda usage must not produce IntPtr findings.
    [Fact]
    public void Allows_Lambdas_And_Linq()
    {
        var results = ValidateContractMethod(@"
    public int Foo()
    {
        var xs = new System.Collections.Generic.List<int> { 1, 2, 3 };
        System.Func<int, int> f = x => x + 1;
        return System.Linq.Enumerable.Sum(System.Linq.Enumerable.Select(xs, f));
    }");
        Assert.DoesNotContain(results, r => r.Info != null && r.Info.Type == "IntPtr");
        Assert.DoesNotContain(results, r => r.Info != null && r.Info.ReferencingMethod == "Foo");
    }

    // Compatibility: constructing/inspecting expression trees is safe (Compile/CompileToMethod
    // remain banned by ReflectionValidator), so safe expression usage must pass the whitelist.
    [Fact]
    public void Allows_Safe_Expression_Tree_Use()
    {
        var results = ValidateContractMethod(@"
    public string Foo()
    {
        System.Linq.Expressions.Expression e = System.Linq.Expressions.Expression.Constant(42);
        return e.ToString();
    }");
        Assert.DoesNotContain(results, r => r.Info != null && r.Info.ReferencingMethod == "Foo");
    }

    private System.Collections.Generic.List<ValidationResult> ValidateContractMethod(string method)
    {
        var source = new SourceCodeBuilder("TestContract").AddMethod(method).Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var validator = new WhitelistValidator(new WhitelistProvider());
        return validator.Validate(module, new CancellationToken()).ToList();
    }
}
