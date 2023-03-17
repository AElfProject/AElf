using AElf.CSharp.CodeOps.Patchers.Module.CallAndBranchCounts;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Patchers.Module.SafeLoopAndRecursion;

public class PatcherTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void Patch_Branch_Beq()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                int i = 1;
                while(i == 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		int num = 1;
		while (num == 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Beq);
    }

    [Fact]
    public void Patch_Branch_Beq_S()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                byte i = 1;
                while(i == 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		byte b = 1;
		while (b == 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Beq_S);
    }

    [Fact]
    public void Patch_Branch_Bge()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                int i = 1;
                while(i >= 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		int num = 1;
		while (num >= 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Bge);
    }

    [Fact]
    public void Patch_Branch_Bge_S()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                byte i = 1;
                while(i >= 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		byte b = 1;
		while (b >= 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Bge_S);
    }

    [Fact]
    public void Patch_Branch_Bge_Un()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i >= 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num >= 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Bge_Un);
    }

    [Fact]
    public void Patch_Branch_Bge_Un_S()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i >= 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num >= 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Bge_Un_S);
    }

    [Fact]
    public void Patch_Branch_Bgt()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                int i = 1;
                while(i > 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		int num = 1;
		while (num > 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Bgt);
    }

    [Fact]
    public void Patch_Branch_Bgt_S()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                int i = 1;
                while(i > 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		int num = 1;
		while (num > 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Bgt_S);
    }

    [Fact]
    public void Patch_Branch_Bgt_Un()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i > 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num > 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Bgt_Un);
    }

    [Fact]
    public void Patch_Branch_Bgt_Un_S()
    {
        var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i > 1){
                    DummyMethod();
                }
            }";

        var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num > 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
        DoTest(method, expectedPatchedContractCode, OpCodes.Bgt_Un_S);
    }

    [Fact]
    public void Patch_Branch_Ble()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                int i = 1;
                while(i <= 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		int num = 1;
		while (num <= 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Ble);
    }

    [Fact]
    public void Patch_Branch_Ble_S()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                int i = 1;
                while(i <= 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		int num = 1;
		while (num <= 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Ble_S);
    }

    [Fact]
    public void Patch_Branch_Ble_Un()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i <= 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num <= 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Ble_Un);
    }

    [Fact]
    public void Patch_Branch_Ble_Un_S()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i <= 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num <= 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Ble_Un_S);
    }

    [Fact]
    public void Patch_Branch_Blt()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                int i = 1;
                while(i < 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		int num = 1;
		while (num < 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Blt);
    }

    [Fact]
    public void Patch_Branch_Blt_S()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                int i = 1;
                while(i < 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		int num = 1;
		while (num < 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Blt_S);
    }
    
    [Fact]
    public void Patch_Branch_Blt_Un()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i < 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num < 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Blt_Un);
    }

    [Fact]
    public void Patch_Branch_Blt_Un_S()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i < 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num < 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Blt_Un_S);
    }

    [Fact]
    public void Patch_Branch_Bne_Un()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i != 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num != 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Bne_Un);
    }

    [Fact]
    public void Patch_Branch_Bne_Un_S()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                uint i = 1u;
                while(i != 1){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		uint num = 1u;
		while (num != 1) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Bne_Un_S);
    }

    [Fact]
    public void Patch_Branch_Br()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                while(true){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		while (true) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Br);
    }

    [Fact]
    public void Patch_Branch_Br_S()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                while(true){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		while (true) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Br_S);
    }

    [Fact]
    public void Patch_Branch_Brfalse()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                bool flag = false;
                while(!flag){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		bool flag = false;
		while (!flag) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Brfalse);
    }

    [Fact]
    public void Patch_Branch_Brfalse_S()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                bool flag = false;
                while(!flag){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		bool flag = false;
		while (!flag) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Brfalse_S);
    }

    [Fact]
    public void Patch_Branch_Brtrue()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                bool flag = true;
                while(flag){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		bool flag = true;
		while (flag) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Brtrue);
    }

    [Fact]
    public void Patch_Branch_Brtrue_S()
    {
	    var method = @"
            private void DummyMethod(){}
            public void InfiniteLoop()
            {
                bool flag = true;
                while(flag){
                    DummyMethod();
                }
            }";

	    var expectedPatchedContractCode = @"namespace TestContract;

public class Contract : Container.ContractBase
{
	private void DummyMethod ()
	{
		ExecutionObserverProxy.CallCount ();
	}

	public void InfiniteLoop ()
	{
		ExecutionObserverProxy.CallCount ();
		bool flag = true;
		while (flag) {
			ExecutionObserverProxy.BranchCount ();
			DummyMethod ();
		}
	}

	public Contract ()
	{
		ExecutionObserverProxy.CallCount ();
		base..ctor ();
	}
}
";
	    DoTest(method, expectedPatchedContractCode, OpCodes.Brtrue_S);
    }

    #region Private Helpers

    private static ModuleDefinition ApplyPatch(ModuleDefinition module)
    {
        var patcher = new Patcher();
        patcher.Patch(module);
        return module;
    }

    private void DoTest(string method, string expectedContractCode, OpCode opCode)
    {
        var methodName = "InfiniteLoop";
        var builder = new SourceCodeBuilder("TestContract").AddMethod(method);
        var module = CompileToAssemblyDefinition(builder.Build()).MainModule;
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(x => x.Name == methodName);
        ReplaceOpCodeIfNeeded(methodDefinition, opCode);
        AssertMethodHasOpCode(module, methodName, opCode);
        ApplyPatch(module);
        var patchedCode =
            DecompileType(module.GetAllTypes().Single(t => t.FullName == builder.ContractTypeFullName));
        Assert.Equal(expectedContractCode.CleanCode(), patchedCode.CleanCode());
    }

    private static readonly Dictionary<OpCode, OpCode> LongFormShortFormMap = new Dictionary<OpCode, OpCode>()
    {
        {OpCodes.Beq, OpCodes.Beq_S},
        {OpCodes.Bge, OpCodes.Bge_S},
        {OpCodes.Bge_Un, OpCodes.Bge_Un_S},
        {OpCodes.Bgt, OpCodes.Bgt_S},
        {OpCodes.Bgt_Un, OpCodes.Bgt_Un_S},
        {OpCodes.Ble, OpCodes.Ble_S},
        {OpCodes.Ble_Un, OpCodes.Ble_Un_S},
        {OpCodes.Blt, OpCodes.Blt_S},
        {OpCodes.Blt_Un, OpCodes.Blt_Un_S},
        {OpCodes.Bne_Un, OpCodes.Bne_Un_S},
        {OpCodes.Br, OpCodes.Br_S},
        {OpCodes.Brfalse, OpCodes.Brfalse_S},
        {OpCodes.Brtrue, OpCodes.Brtrue_S}
    };

    private static void ReplaceOpCodeIfNeeded(MethodDefinition method, OpCode opCode)
    {
        var isLongForm = LongFormShortFormMap.TryGetValue(opCode, out var shortFormOpCode);
        if (!isLongForm) return;
        var needReplacement = method.HasBody && method.Body.Instructions.Any(ins => ins.OpCode == shortFormOpCode);
        if (!needReplacement) return;

        var processor = method.Body.GetILProcessor();
        processor.Body.SimplifyMacros();
        var instructions = method.Body.Instructions.Where(i => i.OpCode == shortFormOpCode).ToList();
        // Possible to be empty as already converted in SimplifyMacros
        foreach (var instruction in instructions)
        {
            var longForm = processor.Create(shortFormOpCode, (Instruction) instruction.Operand);
            processor.Replace(instruction, longForm);
        }
    }

    private void AssertMethodHasOpCode(ModuleDefinition module, string method, OpCode opCode)
    {
        var methodDefinition = module.GetAllTypes().SelectMany(t => t.Methods).Single(x => x.Name == method);
        Assert.True(methodDefinition.HasBody);
        Assert.Contains(methodDefinition.Body.Instructions, ins => ins.OpCode == opCode);
    }

    #endregion
}