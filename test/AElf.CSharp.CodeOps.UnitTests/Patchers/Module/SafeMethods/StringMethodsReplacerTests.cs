using AElf.CSharp.CodeOps.Patchers.Module.SafeMethods;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Patchers.Module.SafeMethods;

public class StringMethodsReplacerTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void Patch_Single_Call()
    {
        var source = @"
public class __Default__ { 
    public string Foo(string a, string b)
    {
        return a + b;
    }
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeCalls = FindCalledMethods(method);
        Assert.Single(beforeCalls);
        Assert.Equal("System.String System.String::Concat(System.String,System.String)", beforeCalls.Single());

        ApplyPatch(module);
        var afterCalls = FindCalledMethods(method);
        Assert.Single(afterCalls);
        Assert.Equal("System.String AElf.Sdk.CSharp.AElfString::Concat(System.String,System.String)",
            afterCalls.Single());
    }

    [Fact]
    public void Patch_Single_Call_In_Nested_Type()
    {
        var source = @"
public class OuterClass {
public class __Default__ { 
    public string Foo(string a, string b)
    {
        return a + b;
    }
}
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeCalls = FindCalledMethods(method);
        Assert.Single(beforeCalls);
        Assert.Equal("System.String System.String::Concat(System.String,System.String)", beforeCalls.Single());

        ApplyPatch(module);
        var afterCalls = FindCalledMethods(method);
        Assert.Single(afterCalls);
        Assert.Equal("System.String AElf.Sdk.CSharp.AElfString::Concat(System.String,System.String)",
            afterCalls.Single());
    }

    [Fact]
    public void Patch_Multiple_Calls()
    {
        var source = @"
public class __Default__ { 
    public string Foo(string a, string b)
    {
        var c = a + b;
        return c +""abc"";
    }
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeCalls = FindCalledMethods(method);
        Assert.Equal(2, beforeCalls.Length);
        Assert.Equal("System.String System.String::Concat(System.String,System.String)",
            beforeCalls.Distinct().Single());

        ApplyPatch(module);
        var afterCalls = FindCalledMethods(method);
        Assert.Equal(2, beforeCalls.Length);
        Assert.Equal("System.String AElf.Sdk.CSharp.AElfString::Concat(System.String,System.String)",
            afterCalls.Distinct().Single());
    }

    [Fact]
    public void Patch_Multiple_Calls_In_Nested_Type()
    {
        var source = @"
public class OuterClass{
public class __Default__ { 
    public string Foo(string a, string b)
    {
        var c = a + b;
        return c +""abc"";
    }
}
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeCalls = FindCalledMethods(method);
        Assert.Equal(2, beforeCalls.Length);
        Assert.Equal("System.String System.String::Concat(System.String,System.String)",
            beforeCalls.Distinct().Single());

        ApplyPatch(module);
        var afterCalls = FindCalledMethods(method);
        Assert.Equal(2, beforeCalls.Length);
        Assert.Equal("System.String AElf.Sdk.CSharp.AElfString::Concat(System.String,System.String)",
            afterCalls.Distinct().Single());
    }


    #region Private Helpers

    private static ModuleDefinition ApplyPatch(ModuleDefinition module)
    {
        var replacer = new StringMethodsReplacer();
        replacer.Patch(module);
        return module;
    }

    private static string[] FindCalledMethods(MethodDefinition method)
    {
        return method.Body.Instructions
            .Where(
                ins => ins.OpCode == OpCodes.Call
            ).Select(
                ins => ins.Operand.ToString() ?? ""
            ).ToArray();
    }

    #endregion
}