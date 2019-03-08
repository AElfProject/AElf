using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Types.SmartContract;
using AElf.Types.CSharp;
using Xunit;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.Token
{
    public sealed class TokenContractTest : TokenContractTestBase
    {
        private ContractTester Tester;
        private ECKeyPair spenderKeyPair;

        private Address BasicZeroContractAddress;
        private Address TokenContractAddress;

        public TokenContractTest()
        {
            Tester = new ContractTester();
            AsyncHelper.RunSync(() => Tester.InitialChainAsync(Tester.GetDefaultContractTypes().ToArray()));
            BasicZeroContractAddress = Tester.GetContractAddress(typeof(BasicContractZero));
            TokenContractAddress = Tester.GetContractAddress(typeof(TokenContract));
            spenderKeyPair = CryptoHelpers.GenerateKeyPair();
        }

        [Fact]
        public async Task Deploy_TokenContract()
        {
            var tx = Tester.GenerateTransaction(BasicZeroContractAddress, nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            await Tester.MineABlockAsync(new List<Transaction> {tx});
            var chain = await Tester.GetChainAsync();
            chain.LongestChainHeight.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Deploy_TokenContract_Twice()
        {
            var bytes1 = await Tester.CallContractMethodAsync(BasicZeroContractAddress, nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            var otherKeyPair = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(otherKeyPair);
            var bytes2 = await Tester.CallContractMethodAsync(BasicZeroContractAddress, nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            bytes1.ShouldNotBeSameAs(bytes2);
        }

        [Fact]
        public async Task Initialize_TokenContract()
        {
            var tx = Tester.GenerateTransaction(TokenContractAddress, nameof(TokenContract.Initialize),
                "ELF", "elf token", 1000_000UL, 2U);
            await Tester.MineABlockAsync(new List<Transaction> {tx});
            var bytes = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.BalanceOf),
                Tester.GetCallOwnerAddress());
            var result = bytes.DeserializeToUInt64();
            result.ShouldBe(1000_000UL);
        }

        [Fact]
        public async Task Initialize_View_TokenContract()
        {
            await Initialize_TokenContract();

            var bytes1 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.TotalSupply));
            bytes1.DeserializeToUInt64().ShouldBe(1000_000UL);
            var bytes2 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Decimals));
            bytes2.DeserializeToUInt64().ShouldBe(2U);
            var byte3 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.TokenName));
            byte3.DeserializeToString().ShouldBe("elf token");
            var byte4 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Symbol));
            byte4.DeserializeToString().ShouldBe("ELF");
        }

        [Fact]
        public async Task Initialize_TokenContract_Failed()
        {
            await Initialize_TokenContract();

            var otherKeyPair = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(otherKeyPair);
            var result = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Initialize),
                "ELF", "elf token", 1000_000UL, 2U);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        [Fact]
        public async Task Transfer_TokenContract()
        {
            await Initialize_TokenContract();

            var toAddress = CryptoHelpers.GenerateKeyPair();
            await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Transfer),
                Tester.GetAddress(toAddress), 1000UL);

            var bytes1 =
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.BalanceOf), Tester.GetCallOwnerAddress());
            var bytes2 =
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.BalanceOf), Tester.GetAddress(toAddress));
            bytes1.DeserializeToUInt64().ShouldBe(1000_000UL - 1000UL);
            bytes2.DeserializeToUInt64().ShouldBe(1000UL);
        }

        [Fact]
        public async Task Transfer_Without_Enough_Token()
        {
            await Initialize_TokenContract();

            var toAddress = CryptoHelpers.GenerateKeyPair();
            var fromAddress = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(fromAddress);

            var result = Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Transfer),
                Tester.GetAddress(toAddress), 1000UL);
            result.Result.Status.ShouldBe(TransactionResultStatus.Failed);
            var bytes = await Tester.CallContractMethodAsync(TokenContractAddress, "BalanceOf",
                Tester.GetAddress(fromAddress));
            var balance = bytes.DeserializeToUInt64();
            result.Result.Error.Contains($"Insufficient balance. Current balance: {balance}").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_TokenContract()
        {
            await Initialize_TokenContract();

            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var result1 = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Approve), spender, 2000UL);
            result1.Status.ShouldBe(TransactionResultStatus.Mined);
            var bytes1 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Allowance), owner, spender);
            bytes1.DeserializeToUInt64().ShouldBe(2000UL);
        }

        [Fact]
        public async Task UnApprove_TokenContract()
        {
            await Approve_TokenContract();
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var result2 =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.UnApprove), spender, 1000UL);
            result2.Status.ShouldBe(TransactionResultStatus.Mined);
            var bytes2 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Allowance), owner, spender);
            bytes2.DeserializeToUInt64().ShouldBe(2000UL - 1000UL);
        }

        [Fact]
        public async Task UnApprove_Without_Enough_Allowance()
        {
            await Initialize_TokenContract();

            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var bytes = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Allowance), owner, spender);
            bytes.DeserializeToUInt64().ShouldBe(0UL);
            var result = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.UnApprove),
                spender, 1000UL);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task TransferFrom_TokenContract()
        {
            await Approve_TokenContract();
            
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            Tester.SetCallOwner(spenderKeyPair);
            var result2 =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.TransferFrom), owner, spender,
                    1000UL);
            result2.Status.ShouldBe(TransactionResultStatus.Mined);
            var bytes2 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Allowance), owner, spender);
            bytes2.DeserializeToUInt64().ShouldBe(2000UL - 1000UL);

            var bytes3 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.BalanceOf), spender);
            bytes3.DeserializeToUInt64().ShouldBe(1000UL);
        }

        [Fact]
        public async Task TransferFrom_With_ErrorAccount()
        {
            await Approve_TokenContract();
            
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var result2 =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.TransferFrom), owner, spender,
                    1000UL);
            result2.Status.ShouldBe(TransactionResultStatus.Failed);
            result2.Error.Contains("Insufficient allowance.").ShouldBeTrue();

            var bytes2 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Allowance), owner, spender);
            bytes2.DeserializeToUInt64().ShouldBe(2000UL);

            var bytes3 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.BalanceOf), spender);
            bytes3.DeserializeToUInt64().ShouldBe(0UL);
        }

        [Fact]
        public async Task TransferFrom_Without_Enough_Allowance()
        {
            await Initialize_TokenContract();     
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var bytes = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Allowance), owner, spender);
            bytes.DeserializeToUInt64().ShouldBe(0UL);

            Tester.SetCallOwner(spenderKeyPair);
            var result =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.TransferFrom), owner, spender,
                    1000UL);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Insufficient allowance.").ShouldBeTrue();
        }

        [Fact]
        public async Task Burn_TokenContract()
        {
            await Initialize_TokenContract();
            await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Burn),
                3000UL);
            var bytes = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.BalanceOf),
                Tester.GetCallOwnerAddress());
            bytes.DeserializeToUInt64().ShouldBe(1000_000UL - 3000UL);
        }

        [Fact]
        public async Task Burn_Without_Enough_Balance()
        {
            await Initialize_TokenContract();
            var burnerAddress = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(burnerAddress);
            var result = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Burn),
                3000UL);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Burner doesn't own enough balance.").ShouldBeTrue();
        }

        [Fact]
        public async Task Charge_Transaction_Fees()
        {
            await Initialize_TokenContract();

            var result =
                await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.ChargeTransactionFees), 10UL);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Transfer),
                Tester.GetAddress(spenderKeyPair), 1000UL);
            var bytes1 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.BalanceOf),
                Tester.GetCallOwnerAddress());
            bytes1.DeserializeToUInt64().ShouldBe(1000_000UL - 1000UL - 10UL);
        }

        [Fact]
        public async Task Claim_Transaction_Fees_Without_FeePoolAddress()
        {
            await Initialize_TokenContract();
            var result = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.ClaimTransactionFees), 1UL);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Fee pool address is not set.").ShouldBeTrue();
        }

        [Fact]
        public async Task Set_And_Get_Method_Fee()
        {
            await Initialize_TokenContract();
            
            var resultGet = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetMethodFee),nameof(TokenContract.Transfer));
            resultGet.DeserializeToUInt64().ShouldBe(0UL);
            
            var resultSet = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.SetMethodFee),nameof(TokenContract.Transfer),10UL);
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var resultGet1 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetMethodFee),nameof(TokenContract.Transfer));
            resultGet1.DeserializeToUInt64().ShouldBe(10UL);
        }
    }
}