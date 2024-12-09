using System.Threading.Tasks;
using AElf.Types;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class TryCatchTests : SolidityContractTestBase
{
    public TryCatchTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        ContractPath = "contracts/TryCatchCallee.contract";
    }

    [Fact]
    public async Task TryCatchTest()
    {
        var calleeContractAddress = await DeployContractAsync();
        ContractPath = "contracts/TryCatchCaller.contract";
        var callerContractAddress = await DeployContractAsync();

        {
            var result = await QueryAsync(
                callerContractAddress,
                "test",
                UInt128Type.From(0)
            );
        }
        {
            var result = await QueryAsync(
                callerContractAddress,
                "test",
                UInt128Type.From(1)
            );
        }

        {
            var result = await QueryAsync(
                callerContractAddress,
                "test",
                UInt128Type.From(2)
            );
        }
        {
            var result = await QueryAsync(
                callerContractAddress,
                "test",
                UInt128Type.From(3)
            );
        }
        {
            var result = await QueryAsync(
                callerContractAddress,
                "test",
                UInt128Type.From(4)
            );
        }
    }
}