using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.ABI.ABIDeserialisation;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class BallotContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> DeployBallotContractTest()
    {
        const string solFilePath = "contracts/Ballot.sol";
        var solidityCode = await File.ReadAllTextAsync(solFilePath);
        var proposals = new List<byte[]>(new[]
        {
            Encoding.UTF8.GetBytes("Proposal #1"),
            Encoding.UTF8.GetBytes("Proposal #2")
        });
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath),
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(
                new ABIValue("bytes32[]", proposals)
            )));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        executionResult.TransactionResult.Logs.Count.ShouldBePositive();
        return executionResult.Output;
    }

    [Fact]
    public async Task ReadProposalsTest()
    {
        var contractAddress = await DeployBallotContractTest();
        var tx = GetTransaction(DefaultSenderKeyPair, contractAddress, "proposals");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldNotBeNull();
    }

    private ABIValue GetBytes32ABIValue(string str)
    {
        return new ABIValue("bytes32", HashHelper.ComputeFrom(str).ToByteArray());
    }
}