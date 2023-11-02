using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;
using Solang;

namespace AElf.Contracts.SolidityContract;

public class HardhatTests : SolidityContractTestBase
{
    [Fact]
    public async Task GetContents()
    {
        const string filePath = "contracts/hardhat/Box.json";
        var json = await File.ReadAllTextAsync(filePath);
        var hardhatOutput = JsonSerializer.Deserialize<HardhatOutput>(json);
        var contracts = hardhatOutput.Input.Sources.Select(s => s.Value.Values).Select(v => v.First()).ToList();
    }

    [Fact]
    public async Task<Address> DeployBoxContractTest()
    {
        const string filePath = "contracts/hardhat/Box.json";
        var solidityCode = ExtraContractCodeFromHardhatOutput(filePath);
        var executionResult = await DeploySolidityContractAsync(solidityCode.GetBytes());
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return executionResult.Output;
    }
}