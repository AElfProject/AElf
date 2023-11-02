using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Types;
using Epoche;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class HashContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> HashSha256Test()
    {
        const string solFilePath = "contracts/Hash.sol";
        var executionResult = await DeploySolidityContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var parameter = new ABIEncode().GetABIEncoded(new ABIValue("string", "hello"));
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "HashSha256");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ToHex().ShouldBe(SHA256.Create().ComputeHash("hello".GetBytes()).ToHex());
        return contractAddress;
    }
    
    [Fact]
    public async Task<Address> HashKeccak256Test()
    {
        const string solFilePath = "contracts/Hash.sol";
        var executionResult = await DeploySolidityContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var parameter = new ABIEncode().GetABIEncoded(new ABIValue("string", "hello"));
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "HashKeccak256");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ToHex().ShouldBe(Keccak256.ComputeHash("hello").ToHex());
        return contractAddress;
    }
}