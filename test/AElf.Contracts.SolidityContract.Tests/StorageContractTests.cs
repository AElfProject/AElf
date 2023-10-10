using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using NBitcoin.DataEncoders;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class StorageContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> StoreTest()
    {
        const string solFilePath = "contracts/Storage.sol";
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var parameter = new ABIEncode().GetABIEncoded(new ABIValue("uint256", 100));
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "store",
            ByteString.CopyFrom(parameter));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return contractAddress;
    }

    [Fact]
    public async Task RetrieveTest()
    {
        var contractAddress = await StoreTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "retrieve");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var hexReturn = txResult.ReturnValue.ToHex();
        var hexValue = Encoders.Hex.EncodeData(new byte[] { 100 });
        hexReturn.ShouldContain(hexValue);
    }
}