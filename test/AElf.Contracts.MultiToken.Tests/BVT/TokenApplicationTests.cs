using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests
    {
        [Fact(DisplayName = "[MultiToken] Transfer token test")]
        public async Task MultiTokenContract_Transfer_Test()
        {
            await MultiTokenContract_Issue_Test();

            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 1000L,
                Memo = "transfer test",
                Symbol = AliceCoinTokenInfo.Symbol,
                To = User1Address
            });

            var defaultBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Owner = DefaultAddress
            });
            var user1BalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Owner = User1Address
            });

            defaultBalanceOutput.Balance.ShouldBe(AliceCoinTotalAmount - 1000L);
            user1BalanceOutput.Balance.ShouldBe(2000L);
        }

        [Fact(DisplayName = "[MultiToken] Transfer token out of total amount")]
        public async Task MultiTokenContract_Transfer_OutOfAmount_Test()
        {
            await MultiTokenContract_Create_Test();

            var result = (await TokenContractStub.Transfer.SendWithExceptionAsync(new TransferInput
            {
                Amount = AliceCoinTotalAmount + 1,
                Memo = "transfer test",
                Symbol = AliceCoinTokenInfo.Symbol,
                To = User2Address
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains($"Insufficient balance").ShouldBeTrue();
        }

        private async Task MultiTokenContract_Approve_Test()
        {
            await MultiTokenContract_Issue_Test();

            var approveResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 2000L,
                Spender = User1Address
            })).TransactionResult;
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1Address,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            balanceOutput.Balance.ShouldBe(1000L);

            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            allowanceOutput.Allowance.ShouldBe(2000L);

            var approveBasisResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 2000L,
                Spender = BasicFunctionContractAddress
            })).TransactionResult;
            approveBasisResult.Status.ShouldBe(TransactionResultStatus.Mined);

        }

        [Fact(DisplayName = "[MultiToken] Approve token test")]
        public async Task MultiTokenContract_Approve_NativeSymbol_Test()
        {
            await MultiTokenContract_Issue_Test();

            var approveResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = "ELF",
                Amount = 2000L,
                Spender = User1Address
            })).TransactionResult;
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1Address,
                Symbol = "ELF"
            });
            balanceOutput.Balance.ShouldBe(0);

            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = "ELF"
            });
            allowanceOutput.Allowance.ShouldBe(2000L);

            var approveBasisResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = "ELF",
                Amount = 2000L,
                Spender = BasicFunctionContractAddress
            })).TransactionResult;
            approveBasisResult.Status.ShouldBe(TransactionResultStatus.Mined);

        }

        [Fact(DisplayName = "[MultiToken] Approve token to Contract")]
        public async Task MultiTokenContract_Approve_ContractAddress_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var approveBasisResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = SymbolForTest,
                Amount = 2000L,
                Spender = BasicFunctionContractAddress
            })).TransactionResult;
            approveBasisResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var basicBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = BasicFunctionContractAddress,
                Symbol = SymbolForTest
            });
            basicBalanceOutput.Balance.ShouldBe(0L);

            var basicAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = BasicFunctionContractAddress,
                Symbol = SymbolForTest
            });
            basicAllowanceOutput.Allowance.ShouldBe(2000L);
        }

        [Fact(DisplayName = "[MultiToken] Approve token out of owner's balance")]
        public async Task MultiTokenContract_Approve_OutOfAmount_Test()
        {
            await MultiTokenContract_Create_Test();

            var approveResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = AliceCoinTotalAmount + 1,
                Spender = User1Address
            })).TransactionResult;

            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact(DisplayName = "[MultiToken] UnApprove token test")]
        public async Task MultiTokenContract_UnApprove_Test()
        {
            await MultiTokenContract_Approve_Test();
            var unApproveBalance = (await TokenContractStub.UnApprove.SendAsync(new UnApproveInput
            {
                Amount = 1000L,
                Symbol = AliceCoinTokenInfo.Symbol,
                Spender = User1Address
            })).TransactionResult;

            unApproveBalance.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            allowanceOutput.Allowance.ShouldBe(2000L - 1000L);
        }

        [Fact]
        public async Task MultiTokenContract_UnApprove_OutOfAmount_Test()
        {
            await MultiTokenContract_Create_Test();

            var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = AliceCoinTokenInfo.Symbol
            });

            allowanceOutput.Allowance.ShouldBe(0L);
            var result = (await TokenContractStub.UnApprove.SendAsync(new UnApproveInput()
            {
                Amount = 1000L,
                Spender = User1Address,
                Symbol = AliceCoinTokenInfo.Symbol
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact(DisplayName = "[MultiToken] Token transferFrom test")]
        public async Task MultiTokenContract_TransferFrom_Test()
        {
            await MultiTokenContract_Approve_Test();
            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result2 = await user1Stub.TransferFrom.SendAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultAddress,
                Memo = "test",
                Symbol = AliceCoinTokenInfo.Symbol,
                To = User1Address
            });
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceOutput2 =
                await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
                {
                    Owner = DefaultAddress,
                    Spender = User1Address,
                    Symbol = AliceCoinTokenInfo.Symbol,
                });
            allowanceOutput2.Allowance.ShouldBe(2000L - 1000L);

            var allowanceOutput3 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1Address,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            allowanceOutput3.Balance.ShouldBe(2000L);
        }

        [Fact(DisplayName = "[MultiToken] Token transferFrom with error account")]
        public async Task MultiTokenContract_TransferFrom_WithErrorAccount_Test()
        {
            await MultiTokenContract_Approve_Test();
            var wrongResult = (await TokenContractStub.TransferFrom.SendWithExceptionAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultAddress,
                Memo = "transfer from test",
                Symbol = AliceCoinTokenInfo.Symbol,
                To = User1Address
            })).TransactionResult;
            wrongResult.Status.ShouldBe(TransactionResultStatus.Failed);
            wrongResult.Error.Contains("Insufficient allowance.").ShouldBeTrue();

            var allowanceOutput2 = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Owner = DefaultAddress,
                Spender = User1Address,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            allowanceOutput2.Allowance.ShouldBe(2000L);

            var balanceOutput3 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1Address,
                Symbol = DefaultSymbol
            });
            balanceOutput3.Balance.ShouldBe(0L);
        }

        private async Task Create_BasicFunctionContract_Issue()
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = SymbolForTest,
                Decimals = 2,
                IsBurnable = true,
                Issuer = DefaultAddress,
                TokenName = "elf test token",
                TotalSupply = DPoSContractConsts.LockTokenForElection * 1000000,
                LockWhiteList =
                {
                    BasicFunctionContractAddress,
                    OtherBasicFunctionContractAddress,
                    TreasuryContractAddress
                }
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = SymbolForTest,
                Amount = DPoSContractConsts.LockTokenForElection * 200000,
                To = DefaultAddress,
                Memo = "Issue"
            });
        }

        [Fact(DisplayName = "[MultiToken] Token lock and unlock test")]
        public async Task MultiTokenContract_LockAndUnLock_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = Address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = Address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(Amount);
            }

            var lockId = Hash.FromString("lockId");

            // Lock.
            var lockTokenResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
            {
                Address = Address,
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
                    Owner = Address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(0);
            }

            // Check locked amount
            {
                var amount = await BasicFunctionContractStub.GetLockedAmount.CallAsync(new GetLockedTokenAmountInput
                {
                    Symbol = SymbolForTest,
                    Address = Address,
                    LockId = lockId,
                });
                amount.Amount.ShouldBe(Amount);
            }

            // Unlock.
            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput
            {
                Address = Address,
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
                    Owner = Address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(Amount);
            }

            //Check amount of lock address after unlocking
            {
                var amount = await BasicFunctionContractStub.GetLockedAmount.CallAsync(new GetLockedTokenAmountInput
                {
                    Symbol = SymbolForTest,
                    Address = Address,
                    LockId = lockId,
                });
                amount.Amount.ShouldBe(0);
            }
        }

        [Fact(DisplayName = "[MultiToken] Token lock through address not in whitelist")]
        public async Task MultiTokenContract_Lock_AddressNotInWhiteList_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = Address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);
            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = Address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(Amount);
            }
            // Try to lock.
            var lockId = Hash.FromString("lockId");
            var defaultSenderStub =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);
            // Lock.
            var lockResult = (await defaultSenderStub.Lock.SendWithExceptionAsync(new LockInput
            {
                Address = Address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            lockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            lockResult.Error.ShouldContain("Not in white list");
        }

        [Fact(DisplayName = "[MultiToken] Token lock with insufficient balance")]
        public async Task MultiTokenContract_Lock_WithInsufficientBalance_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = Address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);
            // Check balance before locking.
            {
                var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = Address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(Amount);
            }

            var lockId = Hash.FromString("lockId");
            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendWithExceptionAsync(new LockTokenInput()
            {
                Address = Address,
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
        [Fact(DisplayName = "[MultiToken] Token unlock until no balance left")]
        public async Task MultiTokenContract_Unlock_repeatedly_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = Address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.FromString("lockId");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = Address,
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
                    Address = Address,
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
                    Address = Address,
                    Amount = Amount / 2,
                    Symbol = SymbolForTest,
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            // Cannot keep on unlocking.
            {
                var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(new UnlockTokenInput()
                {
                    Address = Address,
                    Amount = 1,
                    Symbol = SymbolForTest,
                    LockId = lockId,
                    Usage = "Testing."
                })).TransactionResult;

                unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
                unlockResult.Error.ShouldContain("Insufficient balance");
            }
        }

        [Fact(DisplayName = "[MultiToken] Token unlock excess the total amount of lock")]
        public async Task MultiTokenContract_Unlock_ExcessAmount_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = Address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.FromString("lockId");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = Address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(new UnlockTokenInput()
            {
                Address = Address,
                Amount = Amount + 1,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;
            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact(DisplayName = "[MultiToken] A locked his tokens, B want to unlock with A's lock id'.")]
        public async Task MultiTokenContract_Unlock_NotLocker_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = Address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.FromString("lockId");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = Address,
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
                    Owner = Address,
                    Symbol = SymbolForTest
                });
                result.Balance.ShouldBe(0);
            }

            var unlockResult = (await OtherBasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(new UnlockTokenInput()
            {
                Address = Address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;
            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact(DisplayName =
            "[MultiToken] Unlock the token through strange lockId which is different from locking lockId")]
        public async Task MultiTokenContract_Unlock_StrangeLockId_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = Address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.FromString("lockId");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = Address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(new UnlockTokenInput()
            {
                Address = Address,
                Amount = Amount,
                Symbol = SymbolForTest, 
                LockId = Hash.FromString("lockId1"),
                Usage = "Testing."
            })).TransactionResult;
            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.ShouldContain("Insufficient balance");
        }

        [Fact(DisplayName = "[MultiToken] Unlock the token to another address that isn't the address locked")]
        public async Task MultiTokenContract_Unlock_ToOtherAddress_Test()
        {
            await Create_BasicFunctionContract_Issue();
            var transferResult = (await TokenContractStub.Transfer.SendAsync(new TransferInput()
            {
                Symbol = SymbolForTest,
                Amount = Amount,
                To = Address
            })).TransactionResult;
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var lockId = Hash.FromString("lockId");

            // Lock.
            var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput()
            {
                Address = Address,
                Symbol = SymbolForTest,
                Amount = Amount,
                LockId = lockId,
                Usage = "Testing"
            })).TransactionResult;
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(new UnlockTokenInput()
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

        [Fact(DisplayName = "[MultiToken] Token Burn Test")]
        public async Task MultiTokenContract_Burn_Test()
        {
            await MultiTokenContract_Issue_Test();
            await TokenContractStub.Burn.SendAsync(new BurnInput
            {
                Amount = 3000L,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            balance.Balance.ShouldBe(AliceCoinTotalAmount - 3000L);
        }

        [Fact(DisplayName = "[MultiToken] Token Burn the amount greater than it's amount")]
        public async Task MultiTokenContract_Burn_BeyondBalance_Test()
        {
            await MultiTokenContract_Issue_Test();
            var user1Stub = GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, User1KeyPair);
            var result = (await user1Stub.Burn.SendWithExceptionAsync(new BurnInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 3000L
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Burner doesn't own enough balance.").ShouldBeTrue();
        }

        [Fact(DisplayName = "[MultiToken] Token TransferToContract test")]
        public async Task MultiTokenContract_TransferToContract_Test()
        {
            await MultiTokenContract_Approve_Test();

            var result = (await BasicFunctionContractStub.TransferTokenToContract.SendAsync(
                new TransferTokenToContractInput
                {
                    Amount = 1000L,
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Memo = "TransferToContract test"
                })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var originAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Spender = BasicFunctionContractAddress,
                Owner = DefaultAddress
            });
            originAllowanceOutput.Allowance.ShouldBe(1000L);

            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = BasicFunctionContractAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            balanceOutput.Balance.ShouldBe(1000L);
            
            //allowance not enough
            var result1 = (await BasicFunctionContractStub.TransferTokenToContract.SendAsync(
                new TransferTokenToContractInput
                {
                    Amount = 2000L,
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Memo = "TransferToContract test"
                })).TransactionResult;
            result1.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task MultiTokenContract_SetProfitReceivingInformation_Test()
        {
            await MultiTokenContract_TransferToContract_Test();
            var setResult = (await TokenContractStub.SetProfitReceivingInformation.SendAsync(
                new ProfitReceivingInformation
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DonationPartsPerHundred = 60,
                    ProfitReceiverAddress = DefaultAddress
                })).TransactionResult;
            setResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var receiveInfo =
                await TokenContractStub.GetProfitReceivingInformation.CallAsync(BasicFunctionContractAddress);
            receiveInfo.ProfitReceiverAddress.ShouldBe(DefaultAddress);

        }

        [Fact]
        public async Task MultiTokenContract_ReceiveProfits_Test()
        {
            await MultiTokenContract_SetProfitReceivingInformation_Test();
            await TokenConverter_Converter();
            var tokenOriginBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            })).Balance;

            var result = (await TokenContractStub.ReceiveProfits.SendAsync(new ReceiveProfitsInput
            {
                ContractAddress = BasicFunctionContractAddress,
                Symbols = {AliceCoinTokenInfo.Symbol}
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            var tokenBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            tokenBalanceOutput.Balance.ShouldBe(tokenOriginBalance.Add(1200L));
        }
        
        [Fact]
        public async Task Modify_Available_Token_Test()
        {
            // var managedTokenContractStub =
            //     GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, ManagerKeyPair);
            var parliamentRet = (await ParliamentContractStub.Initialize.SendAsync(new Parliament.InitializeInput
            {
                ProposerAuthorityRequired = true,
                PrivilegedProposer = DefaultAddress
            })).TransactionResult;
            parliamentRet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var createTokenRet = (await TokenContractStub.Create.SendAsync(new CreateInput
            {
                TokenName = "NtELF",
                Decimals = 2,
                IsBurnable = true,
                TotalSupply = 10000_0000,
                Issuer = DefaultAddress,
                Symbol = "EE"
            })).TransactionResult;
            createTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var initResult = (await TokenContractStub.Initialize.SendAsync(new InitializeInput
            {
                DefaultProposer = DefaultAddress
            })).TransactionResult;
            initResult.Status.ShouldBe(TransactionResultStatus.Mined);

            const string addTokenSymbol = "MO";
            const int addTokenWeight = 2;
            const int baseTokenWeight = 1;
            var addAvailableTokenProposalSubmitRet = (await TokenContractStub.SubmitAddAvailableTokenInfoProposal.SendAsync(new AvailableTokenInfo
            {
                TokenSymbol = addTokenSymbol,
                AddedTokenWeight = addTokenWeight,
                BaseTokenWeight = baseTokenWeight
            })).TransactionResult;
            addAvailableTokenProposalSubmitRet.Status.ShouldBe(TransactionResultStatus.Mined);

            var allAvailableTokenInfoBefore = await TokenContractStub.GetAvailableTokenInfos.CallAsync(new Empty()); 
            allAvailableTokenInfoBefore.AllAvailableTokens.Count.ShouldBe(0);
            // var addAvailableTokenRet = (await TokenContractStub.AddAvailableTokenInfo.SendAsync(new AvailableTokenInfo
            // {
            //     TokenSymbol = addTokenSymbol,
            //     AddedTokenWeight = addTokenWeight,
            //     BaseTokenWeight = baseTokenWeight
            // })).TransactionResult;
            // addAvailableTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
            //
            // var allAvailableTokenInfo = await TokenContractStub.GetAvailableTokenInfos.CallAsync(new Empty()); 
            // allAvailableTokenInfo.AllAvailableTokens.SingleOrDefault(x => x.TokenSymbol == addTokenSymbol).ShouldNotBeNull();
            //
            // var updateAvailableTokenRet = (await TokenContractStub.UpdateAvailableTokenInfo.SendAsync(new AvailableTokenInfo
            // {
            //     TokenSymbol = addTokenSymbol,
            //     AddedTokenWeight = 5,
            //     BaseTokenWeight = 3
            // })).TransactionResult;
            // updateAvailableTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
            //
            // allAvailableTokenInfo = await TokenContractStub.GetAvailableTokenInfos.CallAsync(new Empty());
            // var updatedTokenInfo =
            //     allAvailableTokenInfo.AllAvailableTokens.Single(x => x.TokenSymbol == addTokenSymbol);
            // updatedTokenInfo.BaseTokenWeight.ShouldBe(3);
            // updatedTokenInfo.AddedTokenWeight.ShouldBe(5);
            //
            // var removeAvailableTokenRet = (await TokenContractStub.RemoveAvailableTokenInfo.SendAsync( new StringValue
            //     {
            //         Value = addTokenSymbol
            //     }
            // )).TransactionResult;
            // removeAvailableTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
            //
            // allAvailableTokenInfo = await TokenContractStub.GetAvailableTokenInfos.CallAsync(new Empty());
            // allAvailableTokenInfo.AllAvailableTokens.Count.ShouldBe(0);
        }
    }
}