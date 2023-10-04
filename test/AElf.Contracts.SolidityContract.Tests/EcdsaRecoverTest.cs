using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.ABI.Decoders;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class EcdsaRecoverTest : SolidityContractTestBase
{
    [Fact]
    public async Task RecoverPublicAddressTest()
    {
        var message = Encoding.UTF8.GetBytes("Test Ecdsa Recover").ComputeHash();
        var key = SampleAccount.Accounts[1].KeyPair.ToEthECKey();
        var signature = key.Sign(message);
        
        const string solFilePath = "contracts/ecdsa_recover.sol";
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var abiParams = new ABIValue[]
        {
            new ABIValue("bytes32", message),
            new ABIValue("uint8", signature.V),
            new ABIValue("bytes32", signature.R),
            new ABIValue("bytes32", signature.S)
        };
        var parameter = new ABIEncode().GetABIEncoded(abiParams);
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "verify", ByteString.CopyFrom(parameter));

        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        var decoder = new AddressTypeDecoder();
        var result = decoder.Decode<Address>(txResult.ReturnValue.Reverse().ToArray()).ToByteArray();
        result.ShouldBe(key.GetPublicAddressAsBytes());
    }
}