using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class BallotContractTests : SolidityContractTestBase
{
    public BallotContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
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

    public async Task<Address> DeployBallotContractTest()
    {
        return await DeployContractAsync(VecType<BytesType>.GetByteStringFrom(
            [
                BytesType.From(_proposals[0]),
                BytesType.From(_proposals[1])
            ]
        ));
    }

    [Fact]
    public async Task ReadChairpersonTest()
    {
        var contractAddress = await DeployBallotContractTest();
        var chairperson = await QueryAsync(contractAddress, "chairperson");
        AddressType.From(chairperson.ToByteArray()).Value.ShouldBe(DefaultSender);
    }

    [Fact]
    public async Task ReadProposalsTest()
    {
        var contractAddress = await DeployBallotContractTest();
        var proposal1 = await QueryAsync(contractAddress, "proposals", UInt256Type.GetByteStringFrom(0));
        proposal1.ToHex().ShouldContain(_proposals[0].ToHex());
    }
}