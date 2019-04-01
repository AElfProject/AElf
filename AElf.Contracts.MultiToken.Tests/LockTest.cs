using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public class LockTest : MultiTokenContractTestBase
    {
        private ContractTester<MultiTokenContractTestAElfModule> Starter { get; set; }

        private Address ConsensusContractAddress => Starter.GetConsensusContractAddress();

        public LockTest()
        {
            Starter = new ContractTester<MultiTokenContractTestAElfModule>();
            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                Issuer = Starter.GetCallOwnerAddress(),
                TotalSupply = DPoSContractConsts.LockTokenForElection * 100,
                LockWhiteSystemContractNameList = {ConsensusSmartContractAddressNameProvider.Name}
            });

            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = "ELF",
                Amount = DPoSContractConsts.LockTokenForElection * 20,
                ToSystemContractName = DividendsSmartContractAddressNameProvider.Name,
                Memo = "Issue ",
            });

            // For testing.
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = "ELF",
                Amount = DPoSContractConsts.LockTokenForElection * 80,
                To = Starter.GetCallOwnerAddress(),
                Memo = "Set dividends.",
            });
            AsyncHelper.RunSync(() => Starter.InitialChainAsync(list =>
            {
                list.AddGenesisSmartContract<DividendContract>(DividendsSmartContractAddressNameProvider.Name);
                list.AddGenesisSmartContract<TokenContract>(TokenSmartContractAddressNameProvider.Name,
                    tokenContractCallList);
            }));
        }

        [Fact]
        public async Task LockAndUnlockTest()
        {
            const long amount = 100;

            var user = GenerateUser();

            var tester = Starter.CreateNewContractTester(user);

            await Starter.TransferTokenAsync(user, amount);

            // Check balance before locking.
            {
                var balance = await Starter.GetBalanceAsync(user);
                balance.ShouldBe(amount);
            }

            var lockId = Hash.Generate();

            // Lock.
            await tester.ExecuteContractWithMiningAsync(tester.GetTokenContractAddress(), nameof(TokenContract.Lock),
                new LockInput
                {
                    From = user,
                    To = ConsensusContractAddress,
                    Amount = amount,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                });

            // Check balance of user after locking.
            {
                var balance = await tester.GetBalanceAsync(user);
                balance.ShouldBe(0);
            }

            // Unlock.
            await tester.ExecuteContractWithMiningAsync(tester.GetTokenContractAddress(), nameof(TokenContract.Unlock),
                new UnlockInput
                {
                    From = user,
                    To = ConsensusContractAddress,
                    Amount = amount,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                });

            // Check balance of user after unlocking.
            {
                var balance = await tester.GetBalanceAsync(user);
                balance.ShouldBe(amount);
            }
        }

        [Fact]
        public async Task Cannot_Lock_To_Address_Not_In_White_List()
        {
            const long amount = 100;

            var user = GenerateUser();

            var tester = Starter.CreateNewContractTester(user);

            await Starter.TransferTokenAsync(user, amount);

            // Try to lock.
            var lockId = Hash.Generate();

            // Lock.
            var transactionResult = await tester.Lock(amount, lockId, Address.Generate());

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Not in white list");
        }

        [Fact]
        public async Task Lock_With_Insufficient_Balance()
        {
            const long amount = 100;

            var tester = await GenerateTesterAndIssueToken(amount);

            var lockId = Hash.Generate();

            // Lock.
            var transactionResult = await tester.Lock(amount * 2, lockId);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Insufficient balance");
        }

        /// <summary>
        /// It's okay to unlock one locked token to get total amount via several times.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Unlock_Twice_To_Get_Total_Amount_Balance()
        {
            const long amount = 100;

            var tester = await GenerateTesterAndIssueToken(amount);

            var lockId = Hash.Generate();

            // Lock.
            await tester.Lock(amount, lockId);

            // Unlock half of the amount at first.
            {
                var transactionResult = await tester.Unlock(amount / 2, lockId);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Unlock another half of the amount.
            {
                var transactionResult = await tester.Unlock(amount / 2, lockId);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Cannot keep on unlocking.
            {
                var transactionResult = await tester.Unlock(1, lockId);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Insufficient balance");
            }
        }

        [Fact]
        public async Task Unlock_With_Excess_Amount()
        {
            const long amount = 100;

            var tester = await GenerateTesterAndIssueToken(amount);

            var lockId = Hash.Generate();

            // Lock.
            await tester.Lock(amount, lockId);

            var transactionResult = await tester.Unlock(amount / 2 + amount, lockId);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact]
        public async Task Unlock_Token_Not_Himself()
        {
            const long amount = 100;

            var tester1 = await GenerateTesterAndIssueToken(amount);
            var tester2 = await GenerateTesterAndIssueToken(amount);

            var lockId = Hash.Generate();

            // Lock.
            await tester1.Lock(amount, lockId);

            var transactionResult = await tester2.Unlock(amount, lockId);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact]
        public async Task Unlock_Token_With_Strange_LockId()
        {
            const long amount = 100;

            var tester = await GenerateTesterAndIssueToken(amount);

            // Lock.
            await tester.Lock(amount, Hash.Generate());

            var transactionResult = await tester.Unlock(amount, Hash.Generate());

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Insufficient balance");
        }

        private async Task<ContractTester<MultiTokenContractTestAElfModule>> GenerateTesterAndIssueToken(long amount)
        {
            var user = GenerateUser();

            var tester = Starter.CreateNewContractTester(user);

            await Starter.TransferTokenAsync(user, amount);

            return tester;
        }

        private static User GenerateUser()
        {
            var callKeyPair = CryptoHelpers.GenerateKeyPair();
            var callAddress = Address.FromPublicKey(callKeyPair.PublicKey);
            var callPublicKey = callKeyPair.PublicKey.ToHex();

            return new User
            {
                KeyPair = callKeyPair,
                Address = callAddress,
                PublicKey = callPublicKey
            };
        }

        private struct User
        {
            public ECKeyPair KeyPair { get; set; }
            public Address Address { get; set; }
            public string PublicKey { get; set; }

            public static implicit operator ECKeyPair(User user)
            {
                return user.KeyPair;
            }

            public static implicit operator Address(User user)
            {
                return user.Address;
            }

            public static implicit operator string(User user)
            {
                return user.PublicKey;
            }
        }
    }
}