using System.Text.Json;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;
using Solang;
using Solang.Extensions;

namespace AElf.Runtime.WebAssembly.Tests;

public class ExecutiveTests : WebAssemblyRuntimeTestBase
{
    private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;

    public ExecutiveTests()
    {
        _hostSmartContractBridgeContextService = GetRequiredService<IHostSmartContractBridgeContextService>();
    }

    [Theory]
    [InlineData(1023, "00")]
    [InlineData(1024, "01")]
    [InlineData(16, "01")]
    [InlineData(33, "00")]
    public async Task IsPowerOf2Test(ulong input, string output)
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "is_power_of_2";

        TryCompile(solFilePath, out var executive, out var solangAbi).ShouldBeTrue();
        var parameter = new ABIEncode().GetABIEncoded(new ABIValue("uint256", input));
        var txContext = MockTransactionContext(solangAbi!.GetSelector(functionName), ByteString.CopyFrom(parameter));

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);
        await executive.ApplyAsync(txContext);

        var returnValue = txContext.Trace.ReturnValue;
        returnValue.ShouldNotBeNull();
        returnValue.ToHex().ShouldBe(output);
    }

    [Fact(DisplayName = "Input / SealReturn works.")]
    public async Task FooTest()
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "foo";
        TryCompile(solFilePath, out var executive, out var solangAbi).ShouldBeTrue();
        var txContext = MockTransactionContext(solangAbi!.GetSelector(functionName));

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);

        await executive.ApplyAsync(txContext);
        var hexReturn = txContext.Trace.ReturnValue.ToHex();
        hexReturn.ShouldBe("02000000");
    }

    [Fact(DisplayName = "GetStorage / SetStorage works.")]
    public async Task BarTest()
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "bar";
        TryCompile(solFilePath, out var executive, out var solangAbi).ShouldBeTrue();
        var txContext = MockTransactionContext(solangAbi!.GetSelector(functionName));

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive!.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);

        await executive.ApplyAsync(txContext);
        txContext.Trace.ReturnValue.ToHex().ShouldBe("02000000");
    }

    [Fact]
    public async Task ConstructorTest()
    {
        const string solFilePath = "solFiles/Ballot2.sol";
        var proposals = new List<byte[]>(new[]
        {
            "Proposal #1".GetBytes(),
            "Proposal #2".GetBytes()
        });
        TryCompile(solFilePath, out var executive, out var solangAbi).ShouldBeTrue();
        // var txContext = MockTransactionContext("deploy",
        //     ByteString.CopyFrom(new ABIEncode().GetABIEncoded(
        //         new ABIValue("bytes32[]", proposals)
        //     )));
        var txContext = MockTransactionContext("cdbf608d");

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);
        await executive.ApplyAsync(txContext);

        txContext = MockTransactionContext(solangAbi!.GetSelector("setProposals"),
            ByteString.CopyFrom(new ABIEncode().GetABIEncodedPacked(
                new ABIValue("bytes32[]", proposals)
            )));
        await executive.ApplyAsync(txContext);

        // TODO: Read proposals.
    }

    private bool TryCompile(string solFilePath, out IExecutive? executive, out SolangABI? solangAbi)
    {
        executive = null;
        solangAbi = null;
        try
        {
            var code = File.ReadAllBytes(solFilePath);
            var compiledContract = new Compiler().BuildWasm(code);
            executive = new Executive(new UnitTestExternalEnvironment(), compiledContract.Contracts.First());
            solangAbi = JsonSerializer.Deserialize<SolangABI>(compiledContract.Contracts.First().Abi);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}