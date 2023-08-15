using AElf.Kernel;
using AElf.Kernel.SmartContract;
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
        var code = await File.ReadAllBytesAsync(solFilePath);
        var wasmCode = new Compiler().BuildWasm(code).Contracts.First().WasmCode.ToByteArray();
        var executive = new Executive(new UnitTestExternalEnvironment(), wasmCode);

        var txContext = new TransactionContext
        {
            Transaction = new Transaction
            {
                MethodName = functionName.ToSelector(),
                Params = new UInt64Value { Value = input }.ToByteString()
            },
            Trace = new TransactionTrace()
        };
        await executive.ApplyAsync(txContext);
        var returnValue = txContext.Trace.ReturnValue;
        returnValue.ShouldNotBeNull();
        Convert.ToHexString(returnValue.ToByteArray()).ShouldBe(output);
    }
}