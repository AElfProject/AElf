using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Solang;

namespace AElf.Runtime.WebAssembly.Tests;

public class ExecutiveTests : WebAssemblyRuntimeTestBase
{
    [Theory]
    [InlineData(1023, "00")]
    [InlineData(1024, "01")]
    [InlineData(16, "01")]
    [InlineData(33, "00")]
    public async Task IsPowerOf2Test(ulong input, string output)
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "is_power_of_2(uint256)";

        var executive = CreateExecutive(solFilePath);
        var txContext = MockTransactionContext(functionName, new UInt64Value { Value = input }.ToByteString());
        await executive.ApplyAsync(txContext);

        var returnValue = txContext.Trace.ReturnValue;
        returnValue.ShouldNotBeNull();
        returnValue.ToHex().ShouldBe(output);
    }

    [Fact(DisplayName = "Input / SealReturn works.")]
    public async Task FooTest()
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "foo()";
        var executive = CreateExecutive(solFilePath);
        var txContext = MockTransactionContext(functionName);
        await executive.ApplyAsync(txContext);
        var hexReturn = txContext.Trace.ReturnValue.ToHex();
        hexReturn.ShouldBe("02000000");
    }

    [Fact]
    public async Task BarTest()
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "bar()";
        var executive = CreateExecutive(solFilePath);
        var txContext = MockTransactionContext(functionName);
        await executive.ApplyAsync(txContext);
        txContext.Trace.ReturnValue.ToHex().ShouldBe("02000000");
    }

    private IExecutive CreateExecutive(string solFilePath)
    {
        var code = File.ReadAllBytes(solFilePath);
        var wasmCode = new Compiler().BuildWasm(code).Contracts.First().WasmCode.ToByteArray();
        var executive = new Executive(new UnitTestExternalEnvironment(), wasmCode);
        return executive;
    }

    private TransactionContext MockTransactionContext(string functionName, ByteString? param = null)
    {
        var tx = new Transaction
        {
            MethodName = functionName.ToSelector(),
            Params = param ?? ByteString.Empty
        };
        return new TransactionContext
        {
            Transaction = tx,
            Trace = new TransactionTrace()
        };
    }
}