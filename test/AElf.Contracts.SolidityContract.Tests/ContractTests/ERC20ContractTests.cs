using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Scale;
using Shouldly;
using Xunit.Abstractions;
using AddressType = Scale.AddressType;

namespace AElf.Contracts.SolidityContract;

/// <summary>
/// https://github.com/hyperledger/solang/blob/main/integration/polkadot/UniswapV2ERC20.spec.ts
/// </summary>
public class ERC20ContractTests : SolidityContractTestBase
{
    private readonly ITestOutputHelper _outputHelper;

    protected static ECKeyPair AliceKeyPair => SampleAccount.Accounts[0].KeyPair;
    protected static ECKeyPair DaveKeyPair => SampleAccount.Accounts[1].KeyPair;

    protected readonly ABIValue Alice = SampleAccount.Accounts[0].Address.ToWebAssemblyAddress();
    protected readonly ABIValue Dave = SampleAccount.Accounts[1].Address.ToWebAssemblyAddress();

    protected readonly Address AliceAddress = SampleAccount.Accounts[0].Address;
    protected readonly Address DaveAddress = SampleAccount.Accounts[1].Address;

    public ERC20ContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _outputHelper = outputHelper;
    }

    protected const long TotalSupply = (long)1000e10;
    protected const long TestAmount = (long)1e10;

    [Fact(DisplayName = "name, symbol, decimals, totalSupply, balanceOf, DOMAIN_SEPARATOR, PERMIT_TYPEHASH")]
    public async Task<Address> DeployERC20ContractTest()
    {
        var wasmCode = await LoadWasmContractCode("contracts/ERC20.contract");
        var executionResult = await DeployWasmContractAsync(wasmCode,
            UInt256Type.GetByteStringFrom(TotalSupply));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var contractAddress = executionResult.Output;

        // TODO: It's weird the first byte seems like incorrect.
        (await QueryAsync(contractAddress, "name")).ToByteArray()[1..].ShouldBe("Uniswap V2".GetBytes());
        (await QueryAsync(contractAddress, "symbol")).ToByteArray()[1..].ShouldBe("UNI-V2".GetBytes());
        (await QueryAsync(contractAddress, "decimals")).ShouldBe(new byte[] { 18 });
        (await QueryAsync(contractAddress, "totalSupply")).ToByteArray().ToInt64(false)
            .ShouldBe(TotalSupply);
        (await QueryAsync(contractAddress, "balanceOf", Alice.ToParameter())).ToByteArray().ToInt64(false)
            .ShouldBe(TotalSupply);
        var domainSeparator = (await QueryAsync(contractAddress, "DOMAIN_SEPARATOR")).ToByteArray();
        domainSeparator.ShouldNotBeEmpty();
        (await QueryAsync(contractAddress, "PERMIT_TYPEHASH")).ToHex()
            .ShouldBe("6e71edae12b1b97f4d1f60370fef10105fa2faae0126114a169c64845d6126c9");

        return contractAddress;
    }

    [Fact(DisplayName = "approve")]
    public async Task ApproveTest()
    {
        var contractAddress = await DeployERC20ContractTest();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "approve",
            TupleType<AddressType, UInt256Type>.GetByteStringFrom(
                AddressType.From(DaveAddress.ToByteArray()),
                UInt256Type.From(TestAmount)
            ));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var allowance = await QueryAsync(contractAddress, "allowance",
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Alice, Dave)));
        allowance.ToByteArray().ToInt64(false).ShouldBe(TestAmount);
    }

    [Fact(DisplayName = "transfer")]
    public async Task TransferTest()
    {
        var contractAddress = await DeployERC20ContractTest();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "transfer",
            TupleType<AddressType, UInt256Type>.GetByteStringFrom(
                AddressType.From(DaveAddress.ToByteArray()),
                UInt256Type.From(TestAmount)
            ));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        (await QueryAsync(contractAddress, "balanceOf", Dave.ToParameter()))
            .ToByteArray().ToInt64(false).ShouldBe(TestAmount);
    }

    // 0a40
    // b488652ba6d7f056a9afd7a414a9047b94dec5ef8170c26922351c36dc2896fa
    // 00e40b5402000000000000000000000000000000000000000000000000000000

    [Fact(DisplayName = "transferFrom")]
    public async Task TransferFromTest()
    {
        var contractAddress = await DeployERC20ContractTest();

        {
            var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "approve",
                TupleType<AddressType, UInt256Type>.GetByteStringFrom(
                    AddressType.From(DaveAddress.ToByteArray()),
                    UInt256Type.From(TestAmount)
                ));
            await TestTransactionExecutor.ExecuteAsync(tx);
        }

        {
            var tx = await GetTransactionAsync(DaveKeyPair, contractAddress, "transferFrom",
                TupleType<AddressType, AddressType, UInt256Type>.GetByteStringFrom(
                    AddressType.From(AliceAddress.ToByteArray()),
                    AddressType.From(DaveAddress.ToByteArray()),
                    UInt256Type.From(TestAmount)
                ));
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        (await QueryAsync(contractAddress, "balanceOf", Dave.ToParameter())).ToByteArray().ToInt64(false)
            .ShouldBe(TestAmount);
        var allowance = await QueryAsync(contractAddress, "allowance",
            TupleType<AddressType, AddressType>.GetByteStringFrom(
                AddressType.From(AliceAddress.ToByteArray()),
                AddressType.From(DaveAddress.ToByteArray())
            ));
        UInt256Type.From(allowance.ToByteArray()).Value.ShouldBe(0);
    }
}