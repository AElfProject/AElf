using AElf.CSharp.CodeOps.Validators.Method;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Method;

public class ReflectionValidatorTests : CSharpCodeOpsTestBase
{
    // ---- Must be REJECTED: dynamic dispatch / dynamic code loading -------------------------------

    [Fact]
    public void Rejects_Type_InvokeMember_Assembly_Load_Chain()
    {
        // This is exactly the CodeOps bypass: string-based reflection reaches Assembly.Load(byte[]).
        var method = @"
    public string Foo(byte[] code)
    {
        System.Type t = System.Type.GetType(""System.Reflection.Assembly"");
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.InvokeMethod;
        object r = t.InvokeMember(""Load"", flags, null, null, new object[] { code });
        return r.ToString();
    }";
        var errors = Validate(method);
        // Both Type.GetType(string) and Type.InvokeMember are flagged.
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("InvokeMember"));
        Assert.Contains(errors, e => e.Contains("GetType"));
    }

    [Fact]
    public void Rejects_Activator_CreateInstance()
    {
        var method = @"
    public string Foo()
    {
        object o = System.Activator.CreateInstance(typeof(object));
        return o.ToString();
    }";
        var errors = Validate(method);
        Assert.Contains(errors, e => e.Contains("Activator"));
    }

    [Fact]
    public void Rejects_IReflect_InvokeMember()
    {
        // typeof(Assembly) (allowed, it is what typeof lowers to) + cast to IReflect reaches the
        // same InvokeMember dynamic dispatch while declaring the call on a different type.
        var method = @"
    public string Foo(byte[] code)
    {
        System.Type t = typeof(System.Reflection.Assembly);
        System.Reflection.IReflect r = (System.Reflection.IReflect)t;
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.InvokeMethod;
        return r.InvokeMember(""Load"", flags, null, null, new object[] { code }, null, null, null).ToString();
    }";
        var errors = Validate(method);
        Assert.Contains(errors, e => e.Contains("IReflect"));
    }

    [Fact]
    public void Rejects_IReflect_InvokeMember_In_Deeply_Nested_Type()
    {
        // The auditor feeds methods from ALL nesting depths to method validators; make sure a
        // payload hidden two levels down is still reached.
        var source = new SourceCodeBuilder("TestContract")
            .AddClass(@"
    public class Outer
    {
        public class Inner
        {
            public static string Foo(byte[] code)
            {
                System.Type t = typeof(System.Reflection.Assembly);
                System.Reflection.IReflect r = (System.Reflection.IReflect)t;
                var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.InvokeMethod;
                return r.InvokeMember(""Load"", flags, null, null, new object[] { code }, null, null, null).ToString();
            }
        }
    }", isNestedInContract: true)
            .Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(m => m.Name == "Foo");
        var errors = new ReflectionValidator().Validate(methodDefinition, new CancellationToken())
            .Select(r => r.Message).ToList();
        Assert.Contains(errors, e => e.Contains("IReflect"));
    }

    [Fact]
    public void Rejects_Delegate_CreateDelegate()
    {
        var method = @"
    public string Foo()
    {
        var d = System.Delegate.CreateDelegate(typeof(System.Func<int>), this, ""Foo"");
        return d.ToString();
    }";
        var errors = Validate(method);
        Assert.Contains(errors, e => e.Contains("CreateDelegate"));
    }

    [Fact]
    public void Rejects_Assembly_Load()
    {
        var method = @"
    public string Foo(byte[] code)
    {
        var a = System.Reflection.Assembly.Load(code);
        return a.ToString();
    }";
        var errors = Validate(method);
        Assert.Contains(errors, e => e.Contains("Assembly"));
    }

    [Theory]
    [InlineData("System.Reflection.MethodInfo m = null; return m.Invoke(this, null).ToString();")]
    [InlineData("System.Reflection.PropertyInfo p = null; return p.GetValue(this).ToString();")]
    [InlineData("System.Reflection.FieldInfo f = null; return f.GetValue(this).ToString();")]
    public void Rejects_Reflection_Member_Invocation(string body)
    {
        var method = @"
    public string Foo()
    {
        " + body + @"
    }";
        Assert.NotEmpty(Validate(method));
    }

    // ---- Must be ALLOWED: legitimate patterns that legit / system contracts rely on ---------------

    [Fact]
    public void Allows_typeof_Which_Lowers_To_GetTypeFromHandle()
    {
        // typeof(x) => ldtoken + Type.GetTypeFromHandle. This is what protobuf-generated descriptor
        // code uses; it must keep passing.
        var method = @"
    public bool Foo()
    {
        return typeof(string) == typeof(int);
    }";
        Assert.Empty(Validate(method));
    }

    [Fact]
    public void Allows_Instance_Object_GetType()
    {
        // obj.GetType() resolves to System.Object.GetType (NOT the static System.Type.GetType(string)),
        // so it must not be flagged even though the method name is "GetType".
        var method = @"
    public string Foo(object o)
    {
        var t = o.GetType();
        return t == null ? ""x"" : ""y"";
    }";
        Assert.Empty(Validate(method));
    }

    #region Private Helpers

    private List<string> Validate(string method)
    {
        var source = new SourceCodeBuilder("TestContract").AddMethod(method).Build();
        var module = CompileToAssemblyDefinition(source).MainModule;
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(m => m.Name == "Foo");
        return new ReflectionValidator().Validate(methodDefinition, new CancellationToken())
            .Select(r => r.Message).ToList();
    }

    #endregion
}
