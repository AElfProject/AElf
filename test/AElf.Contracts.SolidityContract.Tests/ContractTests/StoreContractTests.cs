using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Types;
using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class StoreContractTests : SolidityContractTestBase
{
    private readonly Address _contractAddress;

    public StoreContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        const string solFilePath = "contracts/store.sol";
        var executionResult = AsyncHelper.RunSync(async () =>
            await DeployWasmContractAsync(await File.ReadAllBytesAsync(solFilePath)));
        _contractAddress = executionResult.Output;
    }

    [Fact]
    public async Task SetValuesTest()
    {
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, _contractAddress, "set_values");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task GetValuesTest()
    {
        await SetValuesTest();

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, _contractAddress, "get_values1");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var hexReturn = txResult.ReturnValue.Reverse().ToArray().ToHex();

            // u64 = type(uint64).max;
            hexReturn.ShouldContain(ulong.MaxValue.ToBytes().ToHex());
            // u32 = 0xdad0feef;
            hexReturn.ShouldContain("dad0feef");
            // i16 = 0x7ffe;
            hexReturn.ShouldContain("7ffe");
            // i256 = type(int256).max;
            hexReturn.ShouldContain(IntType.MAX_INT256_VALUE.ToHex(false).RemoveHexPrefix());
        }

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, _contractAddress, "get_values2");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var hexReturn = txResult.ReturnValue.Reverse().ToArray().ToHex();

            // u256 = 102;
            hexReturn.ShouldContain(102.ToBytes().ToHex());
            // str = "the course of true love never did run smooth";
            hexReturn.ShouldContain("the course of true love never did run smooth".GetBytes().Reverse().ToArray()
                .ToHex());
            // bytes bs = hex"b00b1e";
            hexReturn.ShouldContain("b00b1e".HexToByteArray().Reverse().ToArray().ToHex());
            // fixedbytes = "ABCD";
            hexReturn.ShouldContain("ABCD".GetBytes().Reverse().ToArray().ToHex());
            // bar = enum_bar.bar2;
            hexReturn.ShouldContain("01");
        }
    }

    [Fact]
    public async Task ClearStorageTest()
    {
        await SetValuesTest();
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, _contractAddress, "do_ops");
            await TestTransactionExecutor.ExecuteAsync(tx);
        }

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, _contractAddress, "get_values1");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var hexReturn = txResult.ReturnValue.Reverse().ToArray().ToHex();
        }
    }
}