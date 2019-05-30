using System.Threading.Tasks;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Types;
using AElf.Kernel.Consensus.AEDPoS;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public class LockTest : MultiTokenContractTestBase
    {
        public Address ConsensusContractAddress =>
            ContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);


        public LockTest()
        {
            AsyncHelper.RunSync(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            {
                // this is needed, NOT GOOD DESIGN, it doesn't matter what code we deploy, all we need is an address
                await DeploySystemSmartContract(KernelConstants.CodeCoverageRunnerCategory, TokenContractCode,
                    ConsensusSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                await DeploySystemSmartContract(KernelConstants.CodeCoverageRunnerCategory, TokenContractCode,
                    DividendSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            }
            {
                // TokenContract
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = TokenContractCode;
                TokenContractAddress = await DeploySystemSmartContract(category, code,
                    TokenConverterSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

                var res0 = await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput()
                {
                    Symbol = DefaultSymbol,
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = DPoSContractConsts.LockTokenForElection * 100,
                    Issuer = DefaultSender,
                    LockWhiteSystemContractNameList = {ConsensusSmartContractAddressNameProvider.Name}
                });
                await TokenContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Symbol = DefaultSymbol,
                    Amount = DPoSContractConsts.LockTokenForElection * 20,
                    ToSystemContractName = DividendSmartContractAddressNameProvider.Name,
                    Memo = "Issue ",
                });
                var res = await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = DefaultSymbol,
                    Amount = DPoSContractConsts.LockTokenForElection * 80,
                    To = DefaultSender,
                    Memo = "Set dividends.",
                });
            }
        }

        [Fact]
        public async Task LockAndUnlockTest()
        {
            const long amount = 100;

            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = DefaultSymbol,
                Amount = amount,
                To = User1Address
            });

            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = User1Address,
                    Symbol = DefaultSymbol
                });
                result.Balance.ShouldBe(amount);
            }

            var lockId = Hash.Generate();

            // Lock.
            await TokenContractStub.Lock.SendAsync(new LockInput
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Amount = amount,
                Symbol = "ELF",
                LockId = lockId,
                Usage = "Testing."
            });

            // Check balance of user after locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = User1Address,
                    Symbol = DefaultSymbol
                });
                result.Balance.ShouldBe(0);
            }

            // Unlock.
            await TokenContractStub.Unlock.SendAsync(new UnlockInput
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Amount = amount,
                Symbol = "ELF",
                LockId = lockId,
                Usage = "Testing."
            });

            // Check balance of user after unlocking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = User1Address,
                    Symbol = DefaultSymbol
                });
                result.Balance.ShouldBe(amount);
            }
        }

        [Fact]
        public async Task Cannot_Lock_To_Address_Not_In_White_List()
        {
            const long amount = 100;

            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = DefaultSymbol,
                Amount = amount,
                To = User1Address
            });

            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = User1Address,
                    Symbol = DefaultSymbol
                });
                result.Balance.ShouldBe(amount);
            }

            // Try to lock.
            var lockId = Hash.Generate();

            // Lock.
            var transactionResult = (await TokenContractStub.Lock.SendAsync(new LockInput
            {
                From = User1Address,
                To = Address.Generate(),
                Amount = amount,
                Symbol = "ELF",
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Not in white list");
        }

        [Fact]
        public async Task Lock_With_Insufficient_Balance()
        {
            const long amount = 100;

            var anotherStub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);

            var lockId = Hash.Generate();

            // Lock.
            var transactionResult = (await anotherStub.Lock.SendAsync(new LockInput()
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Symbol = DefaultSymbol,
                Amount = amount * 2,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;

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

            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = DefaultSymbol,
                Amount = amount,
                To = User1Address
            });

            var lockId = Hash.Generate();

            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            // Lock.
            await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Symbol = DefaultSymbol,
                Amount = amount,
                LockId = lockId,
                Usage = "Testing"
            });

            // Unlock half of the amount at first.
            {
                var transactionResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
                {
                    From = User1Address,
                    To = ConsensusContractAddress,
                    Amount = amount / 2,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Unlock another half of the amount.
            {
                var transactionResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
                {
                    From = User1Address,
                    To = ConsensusContractAddress,
                    Amount = amount / 2,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Cannot keep on unlocking.
            {
                var transactionResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
                {
                    From = User1Address,
                    To = ConsensusContractAddress,
                    Amount = 1,
                    Symbol = "ELF",
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Insufficient balance");
            }
        }

        [Fact]
        public async Task Unlock_With_Excess_Amount()
        {
            const long amount = 100;

            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = DefaultSymbol,
                Amount = amount,
                To = User1Address
            });

            var lockId = Hash.Generate();

            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);

            // Lock.
            await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Symbol = DefaultSymbol,
                Amount = amount,
                LockId = lockId,
                Usage = "Testing"
            });

            var transactionResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Amount = amount + 1,
                Symbol = "ELF",
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact]
        public async Task Unlock_Token_Not_Himself()
        {
            const long amount = 100;

            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = DefaultSymbol,
                Amount = amount,
                To = User1Address
            });

            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);

            var lockId = Hash.Generate();

            // Lock.
            await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Symbol = DefaultSymbol,
                Amount = amount,
                LockId = lockId,
                Usage = "Testing"
            });

            var user2Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User2KeyPair);
            var transactionResult = (await user2Stub.Unlock.SendAsync(new UnlockInput()
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Amount = amount,
                Symbol = "ELF",
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact]
        public async Task Unlock_Token_With_Strange_LockId()
        {
            const long amount = 100;


            await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = DefaultSymbol,
                Amount = amount,
                To = User1Address
            });

            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);

            var lockId = Hash.Generate();

            // Lock.
            await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Symbol = DefaultSymbol,
                Amount = amount,
                LockId = lockId,
                Usage = "Testing"
            });

            var transactionResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
            {
                From = User1Address,
                To = ConsensusContractAddress,
                Amount = amount,
                Symbol = "ELF",
                LockId = Hash.Generate(),
                Usage = "Testing."
            })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Insufficient balance");
        }
    }
}