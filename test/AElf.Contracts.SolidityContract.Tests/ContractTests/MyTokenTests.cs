using System.Threading.Tasks;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class MyTokenTests : SolidityContractTestBase
{
    private readonly ITestOutputHelper _outputHelper;

    public MyTokenTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _outputHelper = outputHelper;
        ContractPath = "contracts/mytoken.contract";
    }

    [Fact]
    public async Task MyTokenTest()
    {
        var contractAddress = await DeployContractAsync();

        {
            var txResult = await ExecuteTransactionAsync(contractAddress, "test",
                TupleType<AddressType, BoolType>.GetByteStringFrom(
                    AddressType.From(DefaultSender.ToByteArray()),
                    BoolType.From(true)
                ));

            AddressType.From(txResult.ReturnValue.ToByteArray()).Value.ShouldBe(DefaultSender);
        }

        {
            var txResult = await ExecuteTransactionAsync(contractAddress, "test",
                TupleType<AddressType, BoolType>.GetByteStringFrom(
                    AddressType.From(DefaultSender.ToByteArray()),
                    BoolType.From(false)
                ));

            AddressType.From(txResult.ReturnValue.ToByteArray()).Value.ShouldBe(DefaultSender);
        }
    }
}