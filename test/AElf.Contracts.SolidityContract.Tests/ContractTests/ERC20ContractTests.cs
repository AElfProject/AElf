using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.SolidityContract.Extensions;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

/// <summary>
/// https://github.com/hyperledger/solang/blob/main/integration/polkadot/UniswapV2ERC20.spec.ts
/// </summary>
public class ERC20ContractTests : SolidityContractTestBase
{
    private ECKeyPair AliceKeyPair => SampleAccount.Accounts[0].KeyPair;
    private ECKeyPair DaveKeyPair => SampleAccount.Accounts[1].KeyPair;

    private readonly ABIValue _alice = new("bytes32", SampleAccount.Accounts[0].Address.ToByteArray());

    private readonly ABIValue _dave = new("bytes32", SampleAccount.Accounts[1].Address.ToByteArray());

    private readonly ABIValue _totalSupply = new("uint256", 100000000000);
    private readonly ABIValue _testAmount = new("uint256", 100000000);

    [Fact(DisplayName = "name, symbol, decimals, totalSupply, balanceOf, DOMAIN_SEPARATOR, PERMIT_TYPEHASH")]
    public async Task<Address> DeployERC20ContractTest()
    {
        var solidityFilePathList = new List<string>
        {
            "contracts/ERC20.sol",
            "contracts/UniswapV2ERC20.sol",
            "contracts/interfaces/IUniswapV2ERC20.sol",
            "contracts/libraries/SafeMath.sol",
        };

        var solidityCode = solidityFilePathList.Select(p =>
        {
            var code = File.ReadAllText(p);
            return code;
        }).IntegrateContracts();
        var executionResult =
            await DeploySolidityContractAsync(solidityCode.GetBytes(),
                ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_totalSupply)));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var contractAddress = executionResult.Output;

        // TODO: It's weird the first byte seems like incorrect.
        (await ViewField(contractAddress, "name")).ToByteArray()[1..].ShouldBe("Uniswap V2".GetBytes());
        (await ViewField(contractAddress, "symbol")).ToByteArray()[1..].ShouldBe("UNI-V2".GetBytes());
        (await ViewField(contractAddress, "decimals")).ShouldBe(new byte[] { 18 });
        (await ViewField(contractAddress, "totalSupply")).ShouldBe(new ABIEncode().GetABIEncoded(_totalSupply));
        (await ViewField(contractAddress, "balanceOf", ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_alice))))
            .ShouldBe(new ABIEncode().GetABIEncoded(_totalSupply));
        var domainSeparator = (await ViewField(contractAddress, "DOMAIN_SEPARATOR")).ToByteArray();
        domainSeparator.ShouldNotBeEmpty();
        (await ViewField(contractAddress, "PERMIT_TYPEHASH")).ToHex()
            .ShouldBe("6e71edae12b1b97f4d1f60370fef10105fa2faae0126114a169c64845d6126c9");

        return contractAddress;
    }

    [Fact(DisplayName = "approve")]
    public async Task ApproveTest()
    {
        var contractAddress = await DeployERC20ContractTest();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "approve",
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_dave, _testAmount)));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var allowance = await ViewField(contractAddress, "allowance",
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_alice, _dave)));
        allowance.ShouldBe(new ABIEncode().GetABIEncoded(_testAmount));
    }

    [Fact(DisplayName = "transfer")]
    public async Task TransferTest()
    {
        var contractAddress = await DeployERC20ContractTest();
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "transfer",
            ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_dave, _testAmount)));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);

        (await ViewField(contractAddress, "balanceOf", ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_dave))))
            .ShouldBe(new ABIEncode().GetABIEncoded(_testAmount));
    }

    [Fact(DisplayName = "transferFrom")]
    public async Task TransferFromTest()
    {
        var contractAddress = await DeployERC20ContractTest();

        {
            var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "approve",
                ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_dave, _testAmount)));
            await TestTransactionExecutor.ExecuteAsync(tx);
        }

        {
            var tx = await GetTransactionAsync(DaveKeyPair, contractAddress, "transferFrom",
                ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_alice, _dave, _testAmount)));
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        (await ViewField(contractAddress, "balanceOf", ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_dave))))
            .ShouldBe(new ABIEncode().GetABIEncoded(_testAmount));
        (await ViewField(contractAddress, "allowance",
                ByteString.CopyFrom(new ABIEncode().GetABIEncoded(_alice, _dave))))
            .ShouldBe(new ABIEncode().GetABIEncoded(new ABIValue("uint256", 0)));
    }
}