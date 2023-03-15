using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.Patchers.Module.StaticFieldsReset;

public class PatcherTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void Patch_Separate_Class()
    {
        var anotherClass = @"
public class AnotherClass {
    static byte a;
    static short b;
    static int c;
    static long d;
    static ushort e;
    static uint f;
    static ulong g;
    static decimal h;
    static char i;
    static int j;
    static string k;
}";
        var expectedPatchedCode = @"namespace TestContract;

public class AnotherClass
{
	private static byte a;

	private static short b;

	private static int c;

	private static long d;

	private static ushort e;

	private static uint f;

	private static ulong g;

	private static decimal h;

	private static char i;

	private static int j;

	private static string k;

	public static void ResetFields ()
	{
		//IL_0018: Expected I8, but got I4
		//IL_002a: Expected I8, but got I4
		//IL_0030: Expected O, but got I4
		a = 0;
		b = 0;
		c = 0;
		d = 0L;
		e = 0;
		f = 0u;
		g = 0uL;
		h = 0m;
		i = '\0';
		j = 0;
		k = null;
	}
}
";
        
        var builder = new SourceCodeBuilder("TestContract").AddClass(anotherClass);
        var asm = CompileToAssemblyDefinition(builder.Build());
        var module = asm.MainModule;
        ApplyPatch(module);
        var patchedCode = DecompileType(module.GetAllTypes().Single(t => t.Name == "AnotherClass"));
        Assert.Equal(expectedPatchedCode.CleanCode(), patchedCode.CleanCode());

        
        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	public void ResetFields ()
	{
		AnotherClass.ResetFields ();
	}
}
";
        var patchedContractCode = DecompileType(module.GetAllTypes().Single(t => t.FullName == builder.ContractTypeFullName));
        Assert.Equal(expectedPatchedContractCode.CleanCode(), patchedContractCode.CleanCode());
    }

    [Fact]
    public void Patch_Nested_Class()
    {
	    var anotherClass = @"
public class OuterMostClass{
	static string a;
public class OuterClass{
	static string a;
public class AnotherClass {
    static byte a;
    static short b;
    static int c;
    static long d;
    static ushort e;
    static uint f;
    static ulong g;
    static decimal h;
    static char i;
    static int j;
    static string k;
}
}
}";
	    var expectedPatchedCode = @"namespace TestContract;

public class OuterMostClass
{
	public class OuterClass
	{
		public class AnotherClass
		{
			private static byte a;

			private static short b;

			private static int c;

			private static long d;

			private static ushort e;

			private static uint f;

			private static ulong g;

			private static decimal h;

			private static char i;

			private static int j;

			private static string k;

			public static void ResetFields ()
			{
				//IL_0018: Expected I8, but got I4
				//IL_002a: Expected I8, but got I4
				//IL_0030: Expected O, but got I4
				a = 0;
				b = 0;
				c = 0;
				d = 0L;
				e = 0;
				f = 0u;
				g = 0uL;
				h = 0m;
				i = '\0';
				j = 0;
				k = null;
			}
		}

		private static string a;

		public static void ResetFields ()
		{
			a = null;
			AnotherClass.ResetFields ();
		}
	}

	private static string a;

	public static void ResetFields ()
	{
		a = null;
		OuterClass.ResetFields ();
	}
}
";
        
	    var builder = new SourceCodeBuilder("TestContract").AddClass(anotherClass);
	    var asm = CompileToAssemblyDefinition(builder.Build());
	    var module = asm.MainModule;
	    ApplyPatch(module);
	    var patchedCode = DecompileType(module.GetAllTypes().Single(t => t.Name == "AnotherClass"));

	    Assert.Equal(expectedPatchedCode.CleanCode(), patchedCode.CleanCode());

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	public void ResetFields ()
	{
		OuterMostClass.ResetFields ();
	}
}
";
	    var patchedContractCode = DecompileType(module.GetAllTypes().Single(t => t.FullName == builder.ContractTypeFullName));
	    Assert.Equal(expectedPatchedContractCode.CleanCode(), patchedContractCode.CleanCode());
    }

    [Fact]
    public void Patch_Nested_In_Contract()
    {
	    var anotherClass = @"
public class OuterMostClass{
	static string a;
public class OuterClass{
	static string a;
public class AnotherClass {
    static byte a;
    static short b;
    static int c;
    static long d;
    static ushort e;
    static uint f;
    static ulong g;
    static decimal h;
    static char i;
    static int j;
    static string k;
}
}
}";
	    var expectedPatchedCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	public class OuterMostClass
	{
		public class OuterClass
		{
			public class AnotherClass
			{
				private static byte a;

				private static short b;

				private static int c;

				private static long d;

				private static ushort e;

				private static uint f;

				private static ulong g;

				private static decimal h;

				private static char i;

				private static int j;

				private static string k;

				public static void ResetFields ()
				{
					//IL_0018: Expected I8, but got I4
					//IL_002a: Expected I8, but got I4
					//IL_0030: Expected O, but got I4
					a = 0;
					b = 0;
					c = 0;
					d = 0L;
					e = 0;
					f = 0u;
					g = 0uL;
					h = 0m;
					i = '\0';
					j = 0;
					k = null;
				}
			}

			private static string a;

			public static void ResetFields ()
			{
				a = null;
				AnotherClass.ResetFields ();
			}
		}

		private static string a;

		public static void ResetFields ()
		{
			a = null;
			OuterClass.ResetFields ();
		}
	}

	public void ResetFields ()
	{
		OuterMostClass.ResetFields ();
	}
}
";
        
	    var builder = new SourceCodeBuilder("TestContract").AddClass(anotherClass, true);
	    var asm = CompileToAssemblyDefinition(builder.Build());
	    var module = asm.MainModule;
	    ApplyPatch(module);
	    var patchedCode = DecompileType(module.GetAllTypes().Single(t => t.Name == "AnotherClass"));

	    Assert.Equal(expectedPatchedCode.CleanCode(), patchedCode.CleanCode());
    }

    #region Private Helpers

    private static ModuleDefinition ApplyPatch(ModuleDefinition module)
    {
        var replacer = new ResetFieldsMethodInjector();
        replacer.Patch(module);
        return module;
    }

    private static string DecompileType(TypeDefinition typ)
    {
        using var stream = new MemoryStream();
        typ.Module.Assembly.Write(stream);
        
        stream.Seek(0, SeekOrigin.Begin);
        var peFile = new PEFile("Contract", stream);
        var resolver = new UniversalAssemblyResolver(
            typeof(ISmartContract).Assembly.Location, true, ".NETSTANDARD"
        );
        resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
        var typeSystem = new DecompilerTypeSystem(peFile, resolver);
        var decompiler = new CSharpDecompiler(typeSystem, new DecompilerSettings());
        var fullName = typ.FullName.Replace("/", "+");
        var syntaxTree = decompiler.DecompileType(new FullTypeName(fullName));
        var decompiled = syntaxTree.ToString().Replace("\r\n", "\n");
        return decompiled;
    }

    #endregion
}