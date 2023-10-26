using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class BallotContractTests : SolidityContractTestBase
{
    private List<byte[]> _proposals = new(new[]
    {
        Encoding.UTF8.GetBytes("Proposal #1"),
        Encoding.UTF8.GetBytes("Proposal #2")
    });

    [Fact]
    public async Task<Address> DeployBallotContractTest()
    {
        const string solFilePath = "contracts/Ballot2.sol";
        var solidityCode = await File.ReadAllBytesAsync(solFilePath);
        var proposals = new List<byte[]>(new[]
        {
            Encoding.UTF8.GetBytes("Proposal #1"),
            Encoding.UTF8.GetBytes("Proposal #2")
        });
        var input = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(new ABIValue("bytes32", proposals[0])));
        var executionResult = await DeployWebAssemblyContractAsync(solidityCode, input);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        executionResult.TransactionResult.Logs.Count.ShouldBePositive();
        return executionResult.Output;
    }

    [Fact]
    public async Task ReadChairpersonTest()
    {
        var contractAddress = await DeployBallotContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "chairperson");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldBe(DefaultSender.Value);
    }

    [Fact]
    public async Task ReadProposalsTest()
    {
        var contractAddress = await DeployBallotContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "proposals");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposals = _proposals[0];
    }
}