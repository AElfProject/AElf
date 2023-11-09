using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Kernel;
using AElf.Runtime.WebAssembly;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Solang;

namespace AElf.Contracts.SolidityContract;

public class UniswapV2ContractTests : ERC20ContractTests
{
    [Fact]
    public async Task UploadUniswapV2PairContract()
    {
        var wasmCode = await LoadWasmContractCode("contracts/UniswapV2Pair.contract");
        var uploadResult = await BasicContractZeroStub.UploadSoliditySmartContract.SendAsync(
            new UploadSoliditySmartContractInput
            {
                Category = KernelConstants.WasmRunnerCategory,
                Code = wasmCode.ToByteString()
            });
        var codeHash = uploadResult.Output;

        var registration = await BasicContractZeroStub.GetSmartContractRegistrationByCodeHash.CallAsync(codeHash);
        registration.ShouldNotBeNull();
    }

    [Fact]
    public async Task<Address> DeployUniswapV2FactoryContract()
    {
        await UploadUniswapV2PairContract();

        var abi = await File.ReadAllTextAsync("contracts/UniswapV2Factory.contract");
        var solangAbi = JsonSerializer.Deserialize<SolangABI>(abi);
        var code = solangAbi.Source.Wasm.HexToByteArray();
        var wasmCode = new WasmContractCode
        {
            Code = ByteString.CopyFrom(code),
            Abi = abi,
            CodeHash = Hash.LoadFromHex(solangAbi.Source.Hash)
        };
        var executionResult =
            await DeployWasmContractAsync(wasmCode, ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Alice)));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var contractAddress = executionResult.Output;

        var feeTo = await QueryField(contractAddress, "feeTo");
        feeTo.ToByteArray().ShouldAllBe(i => i == 0);
        (await QueryField(contractAddress, "feeToSetter")).ShouldBe(
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Alice)));
        (await QueryField(contractAddress, "allPairsLength")).ToByteArray().ShouldAllBe(i => i == 0);
        return contractAddress;
    }

    [Fact]
    public async Task CreatePairTest()
    {
        var token0 = SampleAccount.Accounts[2].Address.ToByteArray();
        var token0AbiValue = new ABIValue("bytes32", token0);
        var token1 = SampleAccount.Accounts[3].Address.ToByteArray();
        var token1AbiValue = new ABIValue("bytes32", token1);
        var tokenPair = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(token0AbiValue, token1AbiValue));
        var contractAddress = await DeployUniswapV2FactoryContract();

        {
            var tx = await GetTransactionAsync(DaveKeyPair, contractAddress, "createPair",
                tokenPair);
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        var pairAddress = await QueryField(contractAddress, "getPair", tokenPair);
        pairAddress.ToByteArray().ShouldAllBe(i => i != 0);
    }
}