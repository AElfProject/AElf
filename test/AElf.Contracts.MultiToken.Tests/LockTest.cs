using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Types;
using AElf.Kernel.Token;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public class LockTest : MultiTokenContractTestBase
    {
        private readonly Address _address = AddressHelper.StringToAddress("address");
        private const string SymbolForTest = "ELFTEST";
        private const long Amount = 100;
        private Address BasicFunctionContractAddress { get; set; }
        private Address OtherBasicFunctionContractAddress { get; set; }
        private BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }
        private BasicFunctionContractContainer.BasicFunctionContractStub OtherBasicFunctionContractStub { get; set; }
        private byte[] BasicFunctionContractCode => Codes.Single(kv => kv.Key.Contains("BasicFunction")).Value;
        private Hash BasicFunctionContractName => Hash.FromString("AElf.TestContractNames.BasicFunction");
        private Hash OtherBasicFunctionContractName => Hash.FromString("AElf.TestContractNames.OtherBasicFunction");


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
                    TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
                
                BasicFunctionContractAddress = await DeploySystemSmartContract(KernelConstants.CodeCoverageRunnerCategory, BasicFunctionContractCode,
                    BasicFunctionContractName, DefaultSenderKeyPair);
                BasicFunctionContractStub =
                    GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(BasicFunctionContractAddress,
                        DefaultSenderKeyPair);
                
                OtherBasicFunctionContractAddress = await DeploySystemSmartContract(KernelConstants.CodeCoverageRunnerCategory, BasicFunctionContractCode,
                    OtherBasicFunctionContractName, DefaultSenderKeyPair);
                OtherBasicFunctionContractStub =
                    GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(OtherBasicFunctionContractAddress,
                        DefaultSenderKeyPair);

                await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput()
                {
                    Symbol = DefaultSymbol,
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = DPoSContractConsts.LockTokenForElection * 100,
                    Issuer = DefaultSender,
                    LockWhiteSystemContractNameList = {BasicFunctionContractName,OtherBasicFunctionContractName}
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
                        BasicFunctionContractAddress,
                        OtherBasicFunctionContractAddress
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
        public async Task Create_Token_Use_Custom_Address()
        {
            var transactionResult = (await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = "TEST",
                Decimals = 2,
                IsBurnable = true,
                Issuer = DefaultSender,
                TokenName = "elf test token",
                TotalSupply = DPoSContractConsts.LockTokenForElection * 100,
                LockWhiteList =
                {
                    User1Address
                }
            })).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Addresses in lock white list should be system contract addresses");
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

            var lockId = Hash.FromString("hash0");

            // Lock.
            var lockTokenResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
            {
                Address = _address,
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
            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput
            {
                Address = _address,
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
            var lockId = Hash.FromString("hash1");

            var defaultSenderStub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

            // Lock.
            var lockResult = (await defaultSenderStub.Lock.SendAsync(new LockInput
            {
                Address = _address,
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

            var lockId = Hash.FromString("hash2");
            
            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = _address,
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

            var lockId = Hash.FromString("hash3");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = _address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Unlock half of the amount at first.
            {
                var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput()
                {
                    Address = _address,
                    Amount = Amount / 2,
                    Symbol = SymbolForTest,
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Unlock another half of the amount.
            {
                var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput()
                {
                    Address = _address,
                    Amount = Amount / 2,
                    Symbol = SymbolForTest,
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Cannot keep on unlocking.
            {
                var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput()
                {
                    Address = _address,
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

            var lockId = Hash.FromString("hash4");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = _address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput()
            {
                Address = _address,
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

            var lockId = Hash.FromString("hash5");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = _address,
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

            var unlockResult = (await OtherBasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput()
            {
                Address = _address,
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
            
            var lockId = Hash.FromString("hash6");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = _address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput()
            {
                Address = _address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = Hash.FromString("hash7"),
                Usage = "Testing."
            })).TransactionResult;

            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.ShouldContain("Insufficient balance");
        }
        
                [Fact]
        public async Task Unlock_Token_To_Other_Address()
        {
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = _address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.FromString("hash8");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = _address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput()
            {
                Address = User2Address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.ShouldContain("Insufficient balance");
        }

    }
}