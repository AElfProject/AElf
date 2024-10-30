using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Scale;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class BallotContractTests : SolidityContractTestBase
{
    public BallotContractTests()
    {
        ContractPath = "contracts/Ballot.contract";
    }

    private readonly List<byte[]> _proposals =
    [
        ..new[]
        {
            HashHelper.ComputeFrom("foo").ToByteArray(),
            HashHelper.ComputeFrom("bar").ToByteArray(),
        }
    ];

    [Fact]
    public async Task<Address> DeployBallotContractTest()
    {
        return await DeployContractAsync(TupleType<BytesType, BytesType>.GetByteStringFrom(
            BytesType.From(_proposals[0]),
            BytesType.From(_proposals[1])
        ));
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
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "proposals",
            UInt256Type.GetByteStringFrom(0));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ToHex().ShouldContain(_proposals[0].ToHex());
    }
}