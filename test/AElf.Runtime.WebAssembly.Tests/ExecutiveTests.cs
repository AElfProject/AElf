using AElf.Kernel.SmartContract;
using AElf.Types;
using Shouldly;
using Solang;

namespace AElf.Runtime.WebAssembly.Tests;

public class ExecutiveTests : WebAssemblyRuntimeTestBase
{
    [Fact]
    public async Task IsPowerOf2Test()
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "is_power_of_2(uint256)";
        var code = await File.ReadAllBytesAsync(solFilePath);
        var output = new Compiler().BuildWasm(code);
        var wasmCode = output.Contracts.First().WasmCode.ToByteArray();
        var executive = new Executive(new UnitTestExternalEnvironment(), wasmCode);

        {
            var txContext = new TransactionContext
            {
                Transaction = new Transaction
                {
                    MethodName = functionName,
                }
            };
            await executive.ApplyAsync(txContext);
            Convert.ToHexString(txContext.Trace.ReturnValue.ToByteArray()).ShouldBe("00");
        }
    }
}