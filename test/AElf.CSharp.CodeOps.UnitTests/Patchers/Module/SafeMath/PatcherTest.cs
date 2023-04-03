using Mono.Cecil;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Patchers.Module.SafeMath;

public class PatcherTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void Patch_Add_Single_Occurrence()
    {
        var source = @"
public class __Default__ { 
    public long Foo(long a, long b)
    {
        return a + b;
    }
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: add
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: add.ovf
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }

    [Fact]
    public void Patch_Sub_Single_Occurrence()
    {
        var source = @"
public class __Default__ { 
    public long Foo(long a, long b)
    {
        return a - b;
    }
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: sub
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: sub.ovf
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }

    [Fact]
    public void Patch_Mul_Single_Occurrence()
    {
        var source = @"
public class __Default__ { 
    public long Foo(long a, long b)
    {
        return a * b;
    }
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: mul
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: mul.ovf
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }


    [Fact]
    public void Patch_Add_Multiple_Occurrences()
    {
        var source = @"
public class __Default__ { 
    public long Foo(long a, long b)
    {
        var c = a + b;
        return c + 10;
    }
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: add
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: add
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: add.ovf
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: add.ovf
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }


    [Fact]
    public void Patch_Sub_Multiple_Occurrences()
    {
        var source = @"
public class __Default__ { 
    public long Foo(long a, long b)
    {
        var c = a - b;
        return c - 10;
    }
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: sub
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: sub
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: sub.ovf
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: sub.ovf
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }

    [Fact]
    public void Patch_Mul_Multiple_Occurrences()
    {
        var source = @"
public class __Default__ { 
    public long Foo(long a, long b)
    {
        var c = a * b;
        return c * 10;
    }
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: mul
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: mul
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: mul.ovf
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: mul.ovf
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }

    [Fact]
    public void Patch_Add_Single_Occurrence_In_Nested_Type()
    {
        var source = @"
public class OuterClass {
public class __Default__ { 
    public long Foo(long a, long b)
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
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: add
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: add.ovf
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }

    [Fact]
    public void Patch_Sub_Single_Occurrence_In_Nested_Type()
    {
        var source = @"
public class OuterClass {
public class __Default__ { 
    public long Foo(long a, long b)
    {
        return a - b;
    }
}
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: sub
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: sub.ovf
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }

    [Fact]
    public void Patch_Mul_Single_Occurrence_In_Nested_Type()
    {
        var source = @"
public class OuterClass {
public class __Default__ { 
    public long Foo(long a, long b)
    {
        return a * b;
    }
}
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: mul
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: mul.ovf
IL_0003: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }


    [Fact]
    public void Patch_Add_Multiple_Occurrences_In_Nested_Type()
    {
        var source = @"
public class OuterClass {
public class __Default__ { 
    public long Foo(long a, long b)
    {
        var c = a + b;
        return c + 10;
    }
}
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: add
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: add
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: add.ovf
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: add.ovf
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }


    [Fact]
    public void Patch_Sub_Multiple_Occurrences_In_Nested_Type()
    {
        var source = @"
public class OuterClass {
public class __Default__ { 
    public long Foo(long a, long b)
    {
        var c = a - b;
        return c - 10;
    }
}
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: sub
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: sub
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: sub.ovf
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: sub.ovf
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }

    [Fact]
    public void Patch_Mul_Multiple_Occurrences_In_Nested_Type()
    {
        var source = @"
public class OuterClass {
public class __Default__ { 
    public long Foo(long a, long b)
    {
        var c = a * b;
        return c * 10;
    }
}
}
";
        var asm = CompileToAssemblyDefinition(source);
        var module = asm.MainModule;
        var typ = FindType(module, "__Default__");
        var method = FindMethod(typ, "Foo");
        var beforeIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedBeforeIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: mul
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: mul
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedBeforeIl, beforeIl);

        ApplyPatch(module);
        var afterIl = method.Body.Instructions.Select(ins => ins.ToString()).JoinAsString("\n");
        var expectedAfterIl = @"IL_0000: ldarg.1
IL_0001: ldarg.2
IL_0002: mul.ovf
IL_0003: ldc.i4.s 10
IL_0005: conv.i8
IL_0006: mul.ovf
IL_0007: ret".Replace("\r\n", "\n");
        Assert.Equal(expectedAfterIl, afterIl);
    }

    #region Private Helpers

    private static ModuleDefinition ApplyPatch(ModuleDefinition module)
    {
        var patcher = new AElf.CSharp.CodeOps.Patchers.Module.SafeMath.Patcher();
        patcher.Patch(module);
        return module;
    }

    #endregion
}