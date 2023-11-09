using System.IO;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class ContractTests : SolidityContractTestBase
{
    [Fact(DisplayName = "Deploy array_struct_mapping_storage contract and test")]
    public async Task ArrayStructMappingStorageTest()
    {
        const string solFilePath = "contracts/array_struct_mapping_storage.sol";
        var executionResult = await DeployWasmContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;

        // first set a canary
        var txResult = await ExecuteContractMethod(contractAddress, "setNumber",
            new ABIEncode().GetABIEncoded(new ABIValue("int", 2147483647)));
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    private async Task<TransactionResult> ExecuteContractMethod(Address contractAddress, string methodName,
        byte[] parameter, ECKeyPair keyPair = null)
    {
        var tx = await GetTransactionAsync(keyPair ?? DefaultSenderKeyPair, contractAddress, methodName,
            ByteString.CopyFrom(parameter));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return txResult;
    }
}