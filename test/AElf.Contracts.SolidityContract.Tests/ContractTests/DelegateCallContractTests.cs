using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Scale;
using Shouldly;
using Xunit.Abstractions;
using AddressType = Scale.AddressType;

namespace AElf.Contracts.SolidityContract;

public class DelegateCallContractTests : SolidityContractTestBase
{
    private readonly ITestOutputHelper _outputHelper;

    public DelegateCallContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task DelegateCallTest()
    {
        const long vars = 1616;

        ContractPath = "contracts/Delegatee.contract";
        var delegateeContractAddress = await DeployContractAsync();
        _outputHelper.WriteLine($"Delegatee contract: {delegateeContractAddress}");

        ContractPath = "contracts/Delegator.contract";
        var delegatorContractAddress = await DeployContractAsync();
        _outputHelper.WriteLine($"Delegator contract: {delegatorContractAddress}");

        var txResult = await ExecuteTransactionAsync(delegatorContractAddress, "setVars", 
            TupleType<AddressType, UInt256Type>.GetByteStringFrom(
                AddressType.From(delegateeContractAddress.ToByteArray()),
                UInt256Type.From(vars)
            ), 100);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // Check delegatee contract
        {
            var num = await QueryAsync(delegateeContractAddress, "num");
            UInt256Type.From(num.ToByteArray()).Value.ShouldBe(vars);

            var sender = await QueryAsync(delegateeContractAddress, "sender");
            AddressType.From(sender.ToByteArray()).Value.ShouldBe(DefaultSender);
            
            var value = await QueryAsync(delegateeContractAddress, "value");
            UInt256Type.From(value.ToByteArray()).Value.ShouldBe(100);
        }
        
        // Check delegator contract
        {
            var num = await QueryAsync(delegatorContractAddress, "num");
            UInt256Type.From(num.ToByteArray()).Value.ShouldBe(0);

            var sender = await QueryAsync(delegatorContractAddress, "sender");
            sender.ToByteArray().ShouldAllBe(i => i == 0);
            
            var value = await QueryAsync(delegatorContractAddress, "value");
            UInt256Type.From(value.ToByteArray()).Value.ShouldBe(0);
        }
    }
}