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
        private readonly Address _address = Address.Generate();
        private const string SymbolForTest = "ELFTEST";
        private const long Amount = 100;


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

                await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput()
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
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = DefaultSymbol,
                    Amount = DPoSContractConsts.LockTokenForElection * 80,
                    To = DefaultSender,
                    Memo = "Set dividends.",
                });

                await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = SymbolForTest,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultSender,
                    TokenName = "elf test token",
                    TotalSupply = DPoSContractConsts.LockTokenForElection * 100,
                    LockWhiteList =
                    {
                        User1Address,
                        User2Address
                    }
                });
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = SymbolForTest,
                    Amount = DPoSContractConsts.LockTokenForElection * 20,
                    To = DefaultSender,
                    Memo = "Issue"
                });
            }
        }

        [Fact]
        public async Task LockAndUnlockTest()
        {
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = _address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = _address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(Amount);
            }
            
            var use1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);

            var lockId = Hash.Generate();

            // Lock.
            var lockTokenResult = (await use1Stub.Lock.SendAsync(new LockInput
            {
                From = _address,
                To = User1Address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;
            lockTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check balance of user after locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = _address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(0);
            }

            // Unlock.
            var unlockResult = (await use1Stub.Unlock.SendAsync(new UnlockInput
            {
                From = _address,
                To = User1Address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;
            unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check balance of user after unlocking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = _address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(Amount);
            }
        }

        [Fact]
        public async Task Cannot_Lock_To_Address_Not_In_White_List()
        {
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = _address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = _address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(Amount);
            }

            // Try to lock.
            var lockId = Hash.Generate();

            var use1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);

            // Lock.
            var lockResult = (await use1Stub.Lock.SendAsync(new LockInput
            {
                From = _address,
                To = Address.Generate(),
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            lockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            lockResult.Error.ShouldContain("Not in white list");
        }

        [Fact]
        public async Task Lock_With_Insufficient_Balance()
        {
            var transferResult= (await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = _address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = _address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(Amount);
            }

            var lockId = Hash.Generate();
            
            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            // Lock.
            var lockResult = (await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = _address,
                To = User1Address,
                Symbol = SymbolForTest,
                Amount = Amount * 2,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;

            lockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            lockResult.Error.ShouldContain("Insufficient balance");
        }

        /// <summary>
        /// It's okay to unlock one locked token to get total amount via several times.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Unlock_Twice_To_Get_Total_Amount_Balance()
        {
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = _address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.Generate();

            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            // Lock.
            var lockResult = (await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = _address,
                To = User1Address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Unlock half of the amount at first.
            {
                var unlockResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
                {
                    From = _address,
                    To = User1Address,
                    Amount = Amount / 2,
                    Symbol = SymbolForTest,
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Unlock another half of the amount.
            {
                var unlockResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
                {
                    From = _address,
                    To = User1Address,
                    Amount = Amount / 2,
                    Symbol = SymbolForTest,
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Cannot keep on unlocking.
            {
                var unlockResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
                {
                    From = _address,
                    To = User1Address,
                    Amount = 1,
                    Symbol = SymbolForTest,
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
                unlockResult.Error.ShouldContain("Insufficient balance");
            }
        }

        [Fact]
        public async Task Unlock_With_Excess_Amount()
        {
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = _address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.Generate();

            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            // Lock.
            var lockResult = (await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = _address,
                To = User1Address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
            {
                From = _address,
                To = User1Address,
                Amount = Amount + 1,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact]
        public async Task Unlock_Token_Not_Himself()
        {
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = _address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.Generate();

            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            // Lock.
            var lockResult = (await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = _address,
                To = User1Address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = _address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(0);
            }

            var user2Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User2KeyPair);
            var unlockResult = (await user2Stub.Unlock.SendAsync(new UnlockInput()
            {
                From = _address,
                To = User1Address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact]
        public async Task Unlock_Token_With_Strange_LockId()
        {
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = _address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var lockId = Hash.Generate();

            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            // Lock.
            var lockResult = (await user1Stub.Lock.SendAsync(new LockInput()
            {
                From = _address,
                To = User1Address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = (await user1Stub.Unlock.SendAsync(new UnlockInput()
            {
                From = _address,
                To = User1Address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = Hash.Generate(),
                Usage = "Testing."
            })).TransactionResult;

            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.ShouldContain("Insufficient balance");
        }
    }
}