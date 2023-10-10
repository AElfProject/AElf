using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.Web3.Accounts;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class BallotContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> DeployBallotContractTest()
    {
        const string solFilePath = "contracts/Ballot.sol";
        var solidityCode = await File.ReadAllBytesAsync(solFilePath);
        var proposals = new List<byte[]>(new[]
        {
            Encoding.UTF8.GetBytes("Proposal #1").ComputeHash(),
            Encoding.UTF8.GetBytes("Proposal #2").ComputeHash()
        });
        var input = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(new ABIValue("bytes32", proposals[0])));
        var hexInput = input.ToHex();
        var executionResult = await DeployWebAssemblyContractAsync(solidityCode, input);
        //var executionResult = await DeployWebAssemblyContractAsync(solidityCode);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        executionResult.TransactionResult.Logs.Count.ShouldBePositive();
        return executionResult.Output;
    }

    [Fact]
    public async Task ReadProposalsTest()
    {
        var contractAddress = await DeployBallotContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "proposals");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldNotBeNull();
    }

    private ABIValue GetBytes32ABIValue(string str)
    {
        return new ABIValue("bytes32", HashHelper.ComputeFrom(str).ToByteArray());
    }
}