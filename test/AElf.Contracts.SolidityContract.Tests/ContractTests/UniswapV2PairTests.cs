using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class UniswapV2PairTests : UniswapV2ContractTests
{
    public UniswapV2PairTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {

    }

    private Address _token0Address;
    private Address _token1Address;
    private Address _pairContractAddress;

    private const long MinimumLiquidity = 1000;

    [Fact(DisplayName = "mint")]
    public async Task MintTest()
    {
        const long token0Amount = (long)1e10;
        const long token1Amount = (long)4e10;
        await PrepareAsync();
        await AddLiquidityAsync(token0Amount, token1Amount);

        const long expectedLiquidity = (long)2e10;
        var totalSupply = (await QueryAsync(_pairContractAddress, "totalSupply")).ToByteArray().ToInt64(false);
        totalSupply.ShouldBe(expectedLiquidity);
        (await QueryAsync(_pairContractAddress, "balanceOf", Alice.ToParameter())).ToByteArray().ToInt64(false)
            .ShouldBe(expectedLiquidity - MinimumLiquidity);
        (await QueryAsync(_token0Address, "balanceOf", _pairContractAddress.ToWebAssemblyAddress().ToParameter()))
            .ToByteArray()
            .ToInt64(false).ShouldBe(token0Amount);
        (await QueryAsync(_token1Address, "balanceOf", _pairContractAddress.ToWebAssemblyAddress().ToParameter()))
            .ToByteArray()
            .ToInt64(false).ShouldBe(token1Amount);
        var reserves = (await QueryAsync(_pairContractAddress, "getReserves")).ToByteArray();
        var reserve0 = reserves[..15].ToInt64(false);
        var reserve1 = reserves[16..31].ToInt64(false);
        reserve0.ShouldBe(token0Amount);
        reserve1.ShouldBe(token1Amount);
    }

    [Fact(DisplayName = "swapToken0")]
    public async Task SwapToken0Test()
    {
        const long token0Amount = (long)5e10;
        const long token1Amount = (long)10e10;
        const long swapAmount = (long)1e10;
        const long expectedOutputAmount = 16624979156;
        await PrepareAsync();
        await AddLiquidityAsync(token0Amount, token1Amount);

        await ERC20TransferAsync(AliceKeyPair, _token0Address, _pairContractAddress, swapAmount);

        // Swap
        var tx = await GetTransactionAsync(AliceKeyPair, _pairContractAddress, "swap",
            WebAssemblyTypeHelper.ConvertToParameter(0.ToWebAssemblyUInt256(),
                expectedOutputAmount.ToWebAssemblyUInt256(), Alice, new ABIValue("bytes", new byte[] { 0 })));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var reserves = (await QueryAsync(_pairContractAddress, "getReserves")).ToByteArray();
        var reserve0 = reserves[..15].ToInt64(false);
        var reserve1 = reserves[16..31].ToInt64(false);
        reserve0.ShouldBe(token0Amount + swapAmount);
        reserve1.ShouldBe(token1Amount - expectedOutputAmount);

        (await GetERC20BalanceAsync(_token0Address, _pairContractAddress)).ShouldBe(token0Amount + swapAmount);
        (await GetERC20BalanceAsync(_token1Address, _pairContractAddress))
            .ShouldBe(token1Amount - expectedOutputAmount);

        var token0TotalSupply = (await QueryAsync(_token0Address, "totalSupply")).ToByteArray().ToInt64(false);
        var token1TotalSupply = (await QueryAsync(_token1Address, "totalSupply")).ToByteArray().ToInt64(false);
        (await GetERC20BalanceAsync(_token0Address, AliceAddress))
            .ShouldBe(token0TotalSupply - token0Amount - swapAmount);
        (await GetERC20BalanceAsync(_token1Address, AliceAddress))
            .ShouldBe(token1TotalSupply - token1Amount + expectedOutputAmount);
    }

    [Fact(DisplayName = "swapToken1")]
    public async Task SwapToken1Test()
    {
        const long token0Amount = (long)5e10;
        const long token1Amount = (long)10e10;
        const long swapAmount = (long)1e10;
        const long expectedOutputAmount = 4533054469;
        await PrepareAsync();
        await AddLiquidityAsync(token0Amount, token1Amount);

        await ERC20TransferAsync(AliceKeyPair, _token1Address, _pairContractAddress, swapAmount);

        // Swap
        var tx = await GetTransactionAsync(AliceKeyPair, _pairContractAddress, "swap",
            WebAssemblyTypeHelper.ConvertToParameter(expectedOutputAmount.ToWebAssemblyUInt256(),
                0.ToWebAssemblyUInt256(), Alice, new ABIValue("bytes", new byte[] { 0 })));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var reserves = (await QueryAsync(_pairContractAddress, "getReserves")).ToByteArray();
        var reserve0 = reserves[..15].ToInt64(false);
        var reserve1 = reserves[16..31].ToInt64(false);
        reserve0.ShouldBe(token0Amount - expectedOutputAmount);
        reserve1.ShouldBe(token1Amount + swapAmount);

        (await GetERC20BalanceAsync(_token0Address, _pairContractAddress))
            .ShouldBe(token0Amount - expectedOutputAmount);
        (await GetERC20BalanceAsync(_token1Address, _pairContractAddress)).ShouldBe(token1Amount + swapAmount);

        var token0TotalSupply = (await QueryAsync(_token0Address, "totalSupply")).ToByteArray().ToInt64(false);
        var token1TotalSupply = (await QueryAsync(_token1Address, "totalSupply")).ToByteArray().ToInt64(false);
        (await GetERC20BalanceAsync(_token0Address, AliceAddress))
            .ShouldBe(token0TotalSupply - token0Amount + expectedOutputAmount);
        (await GetERC20BalanceAsync(_token1Address, AliceAddress))
            .ShouldBe(token1TotalSupply - token1Amount - swapAmount);
    }

    [Fact(DisplayName = "burn")]
    public async Task BurnTest()
    {
        const long token0Amount = (long)3e10;
        const long token1Amount = (long)3e10;
        const long expectedOutputAmount = (long)3e10;
        await PrepareAsync();
        await AddLiquidityAsync(token0Amount, token1Amount);

        {
            var tx = await GetTransactionAsync(AliceKeyPair, _pairContractAddress, "transfer_address_uint256",
                WebAssemblyTypeHelper.ConvertToParameter(_pairContractAddress.ToWebAssemblyAddress(),
                    (expectedOutputAmount - MinimumLiquidity).ToWebAssemblyUInt256()));
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var tx = await GetTransactionAsync(AliceKeyPair, _pairContractAddress, "burn",
                Alice.ToParameter());
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        (await GetERC20BalanceAsync(_pairContractAddress, AliceAddress)).ShouldBe(0);
        (await QueryAsync(_pairContractAddress, "totalSupply")).ToByteArray().ToInt64(false).ShouldBe(MinimumLiquidity);

        (await GetERC20BalanceAsync(_token0Address, _pairContractAddress)).ShouldBe(1000);
        (await GetERC20BalanceAsync(_token1Address, _pairContractAddress)).ShouldBe(1000);

        var token0TotalSupply = (await QueryAsync(_token0Address, "totalSupply")).ToByteArray().ToInt64(false);
        var token1TotalSupply = (await QueryAsync(_token1Address, "totalSupply")).ToByteArray().ToInt64(false);
        (await GetERC20BalanceAsync(_token0Address, AliceAddress))
            .ShouldBe(token0TotalSupply - 1000);
        (await GetERC20BalanceAsync(_token1Address, AliceAddress))
            .ShouldBe(token1TotalSupply - 1000);
    }

    private async Task PrepareAsync()
    {
        var factoryContractAddress = await DeployUniswapV2FactoryContract();
        var tokenAContractAddress = await DeployERC20ContractTest();
        var tokenBContractAddress = await DeployERC20ContractTest();
        var tokenPair = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(tokenAContractAddress.ToWebAssemblyAddress(),
            tokenBContractAddress.ToWebAssemblyAddress()));

        {
            var tx = await GetTransactionAsync(AliceKeyPair, factoryContractAddress, "createPair",
                tokenPair);
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        var pairAddressByteString = await QueryAsync(factoryContractAddress, "getPair", tokenPair);
        _pairContractAddress = Address.FromBytes(pairAddressByteString.ToByteArray());

        var token0 = await QueryAsync(_pairContractAddress, "token0");
        var token1 = await QueryAsync(_pairContractAddress, "token1");
        _token0Address = Address.FromBytes(token0.ToByteArray());
        _token1Address = Address.FromBytes(token1.ToByteArray());
    }

    private async Task AddLiquidityAsync(long token0Amount, long token1Amount)
    {
        await ERC20TransferAsync(AliceKeyPair, _token0Address, _pairContractAddress, token0Amount);
        await ERC20TransferAsync(AliceKeyPair, _token1Address, _pairContractAddress, token1Amount);
        // Mint
        {
            var tx = await GetTransactionAsync(AliceKeyPair, _pairContractAddress, "mint",
                Alice.ToParameter());
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var liquidity = txResult.ReturnValue.ToByteArray().ToInt64(false);
            liquidity.ShouldBePositive();
        }
    }

    private async Task ERC20TransferAsync(ECKeyPair fromKeyPair, Address erc20ContractAddress, Address toAddress,
        long amount)
    {
        var fromBalance =
            await GetERC20BalanceAsync(erc20ContractAddress, Address.FromPublicKey(fromKeyPair.PublicKey));
        fromBalance.ShouldBeGreaterThan(amount);
        var beforeTransfer = await GetERC20BalanceAsync(erc20ContractAddress, toAddress);
        var tx = await GetTransactionAsync(fromKeyPair, erc20ContractAddress, "transfer",
            WebAssemblyTypeHelper.ConvertToParameter(toAddress.ToWebAssemblyAddress(),
                amount.ToWebAssemblyUInt256()));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        (await GetERC20BalanceAsync(erc20ContractAddress, toAddress)).ShouldBe(beforeTransfer + amount);
    }

    private async Task<long> GetERC20BalanceAsync(Address erc20ContractAddress, Address ownerAddress)
    {
        return (await QueryAsync(erc20ContractAddress, "balanceOf", ownerAddress.ToWebAssemblyAddress().ToParameter()))
            .ToByteArray().ToInt64(false);
    }
}