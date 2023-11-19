using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class BallotContractTests : SolidityContractTestBase
{
    private readonly List<byte[]> _proposals = new(new[]
    {
        Encoding.UTF8.GetBytes("foo"),
        Encoding.UTF8.GetBytes("bar")
    });

    [Fact]
    public async Task<Address> DeployBallotContractTest()
    {
        const string solFilePath = "contracts/Ballot.sol";
        var solidityCode = await File.ReadAllBytesAsync(solFilePath);
        var input = WebAssemblyTypeHelper.ConvertToParameter(new ABIValue("bytes32[]", new byte[]{}));
        var executionResult = await DeployWasmContractAsync(solidityCode, input);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        executionResult.TransactionResult.Logs.Count.ShouldBePositive();
        return executionResult.Output;
    }

    [Fact]
    public async Task<Address> DeployBallot2ContractTest()
    {
        const string solFilePath = "contracts/Ballot2.sol";
        var solidityCode = await File.ReadAllBytesAsync(solFilePath);
        var input = _proposals[0].ToBytes32ABIValue().ToParameter();
        var executionResult = await DeployWasmContractAsync(solidityCode, input);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return executionResult.Output;
    }

    [Fact]
    public async Task ReadChairpersonTest()
    {
        var contractAddress = await DeployBallot2ContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "chairperson");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldBe(DefaultSender.Value);
    }

    [Fact]
    public async Task ReadProposalsTest()
    {
        var contractAddress = await DeployBallot2ContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "proposals",
            0.ToWebAssemblyUInt256().ToParameter());
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ToHex().ShouldContain(_proposals[0].ToHex());
    }
}