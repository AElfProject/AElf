using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Runtime.WebAssembly;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Solang;

namespace AElf.Contracts.SolidityContract;

/// <summary>
/// https://github.com/hyperledger/solang/blob/main/integration/polkadot/UniswapV2ERC20.spec.ts
/// </summary>
public class ERC20ContractTests : SolidityContractTestBase
{
    protected static ECKeyPair AliceKeyPair => SampleAccount.Accounts[0].KeyPair;
    protected static ECKeyPair DaveKeyPair => SampleAccount.Accounts[1].KeyPair;

    protected readonly ABIValue Alice = new("bytes32", SampleAccount.Accounts[0].Address.ToByteArray());
    protected readonly ABIValue Dave = new("bytes32", SampleAccount.Accounts[1].Address.ToByteArray());

    private readonly ABIValue _totalSupply = new("uint256", 100000000000);
    private readonly ABIValue _testAmount = new("uint256", 100000000);

    [Fact(DisplayName = "name, symbol, decimals, totalSupply, balanceOf, DOMAIN_SEPARATOR, PERMIT_TYPEHASH")]
    public async Task<Address> DeployERC20ContractTest()
    {
        var wasmCode = await LoadWasmContractCode("contracts/ERC20.contract");
        var executionResult = await DeployWasmContractAsync(wasmCode,
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_totalSupply)));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var contractAddress = executionResult.Output;

        // TODO: It's weird the first byte seems like incorrect.
        (await QueryField(contractAddress, "name")).ToByteArray()[1..].ShouldBe("Uniswap V2".GetBytes());
        (await QueryField(contractAddress, "symbol")).ToByteArray()[1..].ShouldBe("UNI-V2".GetBytes());
        (await QueryField(contractAddress, "decimals")).ShouldBe(new byte[] { 18 });
        (await QueryField(contractAddress, "totalSupply")).ShouldBe(new ABIEncode().GetABIEncoded(_totalSupply));
        (await QueryField(contractAddress, "balanceOf", ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Alice))))
            .ShouldBe(new ABIEncode().GetABIEncoded(_totalSupply));
        var domainSeparator = (await QueryField(contractAddress, "DOMAIN_SEPARATOR")).ToByteArray();
        domainSeparator.ShouldNotBeEmpty();
        (await QueryField(contractAddress, "PERMIT_TYPEHASH")).ToHex()
            .ShouldBe("6e71edae12b1b97f4d1f60370fef10105fa2faae0126114a169c64845d6126c9");

        return contractAddress;
    }

    [Fact(DisplayName = "approve")]
    public async Task ApproveTest()
    {
        var contractAddress = await DeployERC20ContractTest();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "approve",
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Dave, _testAmount)));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var allowance = await QueryField(contractAddress, "allowance",
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Alice, Dave)));
        allowance.ShouldBe(new ABIEncode().GetABIEncoded(_testAmount));
    }

    [Fact(DisplayName = "transfer")]
    public async Task TransferTest()
    {
        var contractAddress = await DeployERC20ContractTest();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "transfer",
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Dave, _testAmount)));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);

        (await QueryField(contractAddress, "balanceOf", ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Dave))))
            .ShouldBe(new ABIEncode().GetABIEncoded(_testAmount));
    }

    [Fact(DisplayName = "transferFrom")]
    public async Task TransferFromTest()
    {
        var contractAddress = await DeployERC20ContractTest();

        {
            var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "approve",
                ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Dave, _testAmount)));
            await TestTransactionExecutor.ExecuteAsync(tx);
        }

        {
            var tx = await GetTransactionAsync(DaveKeyPair, contractAddress, "transferFrom",
                ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Alice, Dave, _testAmount)));
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        (await QueryField(contractAddress, "balanceOf", ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Dave))))
            .ShouldBe(new ABIEncode().GetABIEncoded(_testAmount));
        (await QueryField(contractAddress, "allowance",
                ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Alice, Dave))))
            .ShouldBe(new ABIEncode().GetABIEncoded(new ABIValue("uint256", 0)));
    }
}