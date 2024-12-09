using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Scale;
using Shouldly;
using Xunit.Abstractions;
using StringType = Scale.StringType;

namespace AElf.Contracts.SolidityContract;

public class StorageContractTests : SolidityContractTestBase
{
    public StorageContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        ContractPath = "contracts/Storage.contract";
    }

    [Fact(DisplayName = "Store 100 to Storage contract.")]
    public async Task<Address> StoreTest()
    {
        var contractAddress = await DeployContractAsync();
        var txResult = await ExecuteTransactionAsync(contractAddress, "store", UInt256Type.GetByteStringFrom(100));
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return contractAddress;
    }

    [Fact(DisplayName = "Retrieve 100 from Storage contract.")]
    public async Task RetrieveTest()
    {
        var contractAddress = await StoreTest();
        var result = await QueryAsync(contractAddress, "retrieve");
        result.ShouldBe(UInt256Type.GetByteStringFrom(100));
    }
}