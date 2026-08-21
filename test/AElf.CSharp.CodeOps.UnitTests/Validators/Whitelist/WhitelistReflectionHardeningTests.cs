using System.Linq;
using System.Threading;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Whitelist;

/// <summary>
/// Covers the whitelist hardening around reflection:
/// - F1: System.Type metadata remains compatible; dynamic reflection calls are denied by ReflectionValidator.
/// - F2: executable member/type operands and generic arguments are validated.
/// - F3: passive callee/signature/local metadata remains compatible with deployed contracts.
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

    // Compatibility: the formal parameter types of a safe framework call are metadata of the
    // callee, not capabilities owned by the contract. These calls are explicitly whitelisted, but
    // their signatures mention framework enums/interfaces that are intentionally absent from the
    // contract whitelist.
    [Fact]
    public void Allows_Safe_Calls_With_NonWhitelisted_Formal_Parameter_Types()
    {
        var results = ValidateContractMethod(@"
    public bool Foo(string value)
    {
        var parts = value.Split(new[] { "","" }, System.StringSplitOptions.RemoveEmptyEntries);
        var parsed = System.Uri.TryCreate(value, System.UriKind.Absolute, out var uri);
        var number = 1.25m.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return parsed && parts.Length > 0 && number.Length > 0;
    }");
        Assert.DoesNotContain(results, r => r.Info != null && r.Info.ReferencingMethod == "Foo");
    }

    // Compatibility: deployed contracts use Type metadata helpers such as GetEnumName. Dynamic
    // dispatch through Type.GetType/InvokeMember is covered by ReflectionValidatorTests.
    [Fact]
    public void Allows_Safe_Type_Metadata_Access()
    {
        var source = new SourceCodeBuilder("TestContract")
            .AddClass(@"
    public enum SampleValue
    {
        One = 1
    }")
            .AddMethod(@"
    public string Foo()
    {
        var type = typeof(SampleValue);
        var ignored = type.FullName;
        return type.GetEnumName(SampleValue.One);
    }")
            .Build();
        var results = ValidateContractSource(source);
        Assert.DoesNotContain(results, r => r.Info != null && r.Info.ReferencingMethod == "Foo");
    }

    // Compatibility: passive signature and local metadata is not an executable capability. The
    // dangerous paths that can create or invoke these values are checked at their actual IL
    // operands by the whitelist and ReflectionValidator.
    [Fact]
    public void Allows_Passive_Signature_And_Local_Metadata()
    {
        var source = new SourceCodeBuilder("TestContract").AddMethod(@"
    public string Foo(System.Reflection.Binder b)
    {
        return b is null ? ""x"" : ""y"";
    }

    public System.Reflection.Binder ReturnBinder()
    {
        return null;
    }").Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var method = module.GetAllTypes().SelectMany(type => type.Methods)
            .Single(candidate => candidate.Name == "Foo");
        method.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(System.IntPtr))));

        var results = new WhitelistValidator(new WhitelistProvider())
            .Validate(module, new CancellationToken()).ToList();
        Assert.DoesNotContain(results, r => r.Info != null &&
                                            (r.Info.ReferencingMethod == "Foo" ||
                                             r.Info.ReferencingMethod == "ReturnBinder"));
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
    // (object, IntPtr) signature. Callee formal parameters are not contract-owned capabilities,
    // so ordinary lambda usage must not produce IntPtr findings.
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
        return ValidateContractSource(source);
    }

    private System.Collections.Generic.List<ValidationResult> ValidateContractSource(string source)
    {
        var module = CompileToAssemblyDefinition(source).MainModule;
        var validator = new WhitelistValidator(new WhitelistProvider());
        return validator.Validate(module, new CancellationToken()).ToList();
    }
}
