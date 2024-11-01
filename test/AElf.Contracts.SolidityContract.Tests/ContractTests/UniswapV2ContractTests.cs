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
using Scale;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using AddressType = Scale.AddressType;

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

        (await QueryAsync(contractAddress, "feeTo")).ToByteArray().ShouldAllBe(i => i == 0);
        (await QueryAsync(contractAddress, "feeToSetter")).ToByteArray().ShouldBe(AliceAddress.ToByteArray());
        (await QueryAsync(contractAddress, "allPairsLength")).ToByteArray().ToInt64(false).ShouldBe(0);
        return contractAddress;
    }

    [Fact(DisplayName = "createPair")]
    public async Task CreatePairTest()
    {
        var token0Address = SampleAccount.Accounts[3].Address;
        var token1Address = SampleAccount.Accounts[2].Address;
        var tokenPair = TupleType<AddressType, AddressType>.GetByteStringFrom(
            AddressType.From(token0Address.ToByteArray()),
            AddressType.From(token1Address.ToByteArray())
        );
        var factoryContractAddress = await DeployUniswapV2FactoryContract();

        {
            var tx = await GetTransactionAsync(AliceKeyPair, factoryContractAddress, "createPair",
                tokenPair);
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        var pairAddress1 = await QueryAsync(factoryContractAddress, "getPair", tokenPair);
        pairAddress1.ToByteArray().ShouldAllBe(i => i != 0);

        var pairAddress2 = await QueryAsync(factoryContractAddress, "getPair",
            TupleType<AddressType, AddressType>.GetByteStringFrom(
                // Reverse the order of token0 and token1.
                AddressType.From(token1Address.ToByteArray()),
                AddressType.From(token0Address.ToByteArray())
            ));
        pairAddress2.ShouldBe(pairAddress1);

        var pairAddress = await QueryAsync(factoryContractAddress, "allPairs", UInt256Type.GetByteStringFrom(0));
        pairAddress.ShouldBe(pairAddress1);
        var pairContractAddress = Address.FromBytes(pairAddress.ToByteArray());

        var allPairsLength = await QueryAsync(factoryContractAddress, "allPairsLength");
        allPairsLength.ToByteArray().ToInt64(false).ShouldBe(1);

        var factory = await QueryAsync(pairContractAddress, "factory");
        factory.ToByteArray().ShouldBe(factoryContractAddress.ToByteArray());

        var queriedToken0 = await QueryAsync(pairContractAddress, "token0");
        queriedToken0.ToByteArray().ShouldBe(token0Address.ToByteArray());

        var queriedToken1 = await QueryAsync(pairContractAddress, "token1");
        queriedToken1.ToByteArray().ShouldBe(token1Address.ToByteArray());
    }

    [Fact(DisplayName = "setFeeTo")]
    public async Task SetFeeToTest()
    {
        var contractAddress = await DeployUniswapV2FactoryContract();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "setFeeTo",
            Dave.ToParameter());
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var queriedFeeTo = await QueryAsync(contractAddress, "feeTo");
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
        var queriedFeeToSetter = await QueryAsync(contractAddress, "feeToSetter");
        queriedFeeToSetter.ToByteArray().ShouldBe(DaveAddress.ToByteArray());
    }


}