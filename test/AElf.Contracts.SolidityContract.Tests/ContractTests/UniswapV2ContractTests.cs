using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Kernel;
using AElf.Runtime.WebAssembly;
using AElf.Runtime.WebAssembly.Types;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Solang;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class UniswapV2ContractTests : ERC20ContractTests
{
    public UniswapV2ContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {

    }

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

    [Fact(DisplayName = "feeTo, feeToSetter, allPairsLength")]
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
            await DeployWasmContractAsync(wasmCode, Alice.ToParameter());
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var contractAddress = executionResult.Output;

        (await QueryField(contractAddress, "feeTo")).ToByteArray().ShouldAllBe(i => i == 0);
        (await QueryField(contractAddress, "feeToSetter")).ToByteArray().ShouldBe(AliceAddress.ToByteArray());
        (await QueryField(contractAddress, "allPairsLength")).ToByteArray().ToInt64(false).ShouldBe(0);
        return contractAddress;
    }

    [Fact(DisplayName = "createPair")]
    public async Task CreatePairTest()
    {
        var token0 = SampleAccount.Accounts[3].Address.ToByteArray();
        var token0AbiValue = new ABIValue("bytes32", token0);
        var token1 = SampleAccount.Accounts[2].Address.ToByteArray();
        var token1AbiValue = new ABIValue("bytes32", token1);
        var tokenPair = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(token0AbiValue, token1AbiValue));
        var factoryContractAddress = await DeployUniswapV2FactoryContract();

        {
            var tx = await GetTransactionAsync(AliceKeyPair, factoryContractAddress, "createPair",
                tokenPair);
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        var pairAddress1 = await QueryField(factoryContractAddress, "getPair", tokenPair);
        pairAddress1.ToByteArray().ShouldAllBe(i => i != 0);

        var pairAddress2 = await QueryField(factoryContractAddress, "getPair",
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(token1AbiValue, token0AbiValue)));
        pairAddress2.ShouldBe(pairAddress1);

        var pairAddress = await QueryField(factoryContractAddress, "allPairs", 0.ToWebAssemblyUInt256().ToParameter());
        pairAddress.ShouldBe(pairAddress1);
        var pairContractAddress = Address.FromBytes(pairAddress.ToByteArray());

        var allPairsLength = await QueryField(factoryContractAddress, "allPairsLength");
        allPairsLength.ToByteArray().ToInt64(false).ShouldBe(1);

        var factory = await QueryField(pairContractAddress, "factory");
        factory.ToByteArray().ShouldBe(factoryContractAddress.ToByteArray());

        var queriedToken0 = await QueryField(pairContractAddress, "token0");
        queriedToken0.ToByteArray().ShouldBe(token0);

        var queriedToken1 = await QueryField(pairContractAddress, "token1");
        queriedToken1.ToByteArray().ShouldBe(token1);
    }

    [Fact(DisplayName = "setFeeTo")]
    public async Task SetFeeToTest()
    {
        var contractAddress = await DeployUniswapV2FactoryContract();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "setFeeTo",
            Dave.ToParameter());
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var queriedFeeTo = await QueryField(contractAddress, "feeTo");
        queriedFeeTo.ToByteArray().ShouldBe(DaveAddress.ToByteArray());
    }

    [Fact(DisplayName = "setFeeToSetter")]
    public async Task SetFeeToSetterTest()
    {
        var contractAddress = await DeployUniswapV2FactoryContract();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "setFeeToSetter",
            Dave.ToParameter());
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var queriedFeeToSetter = await QueryField(contractAddress, "feeToSetter");
        queriedFeeToSetter.ToByteArray().ShouldBe(DaveAddress.ToByteArray());
    }


}