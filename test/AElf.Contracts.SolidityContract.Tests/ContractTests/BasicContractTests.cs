using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Types;
using Epoche;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class BasicContractTests : SolidityContractTestBase
{
    public BasicContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        ContractPath = "contracts/Basic.contract";
    }
    public async Task<Address> DeployBasicContractTest()
    {
        return await DeployContractAsync();
    }

    [Fact]
    public async Task AddressTest()
    {
        var contractAddress = await DeployBasicContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "getAddress");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldBe(contractAddress.Value);
    }
    
    [Fact]
    public async Task HashSha256Test()
    {
        var contractAddress = await DeployBasicContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "hashSha256");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ToHex().ShouldBe(SHA256.Create().ComputeHash("hello".GetBytes()).ToHex());
    }
    
    [Fact]
    public async Task HashKeccak256Test()
    {
        var contractAddress = await DeployBasicContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "hashKeccak256");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ToHex().ShouldBe(Keccak256.ComputeHash("hello").ToHex());
    }
}