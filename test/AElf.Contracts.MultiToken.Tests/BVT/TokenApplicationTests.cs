using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Standards.ACS10;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
{
    [Fact(DisplayName = "[MultiToken] Transfer token test")]
    public async Task MultiTokenContract_Transfer_Test()
    {
        await CreateAndIssueMultiTokensAsync();

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
        await CreateAndIssueMultiTokensAsync();

        var result = (await TokenContractStub.Transfer.SendWithExceptionAsync(new TransferInput
        {
            Amount = AliceCoinTotalAmount + 1,
            Memo = "transfer test",
            Symbol = AliceCoinTokenInfo.Symbol,
            To = DefaultAddress
        })).TransactionResult;
        result.Error.ShouldContain("Can't do transfer to sender itself");

        result = (await TokenContractStub.Transfer.SendWithExceptionAsync(new TransferInput
        {
            Amount = AliceCoinTotalAmount + 1,
            Memo = "transfer test",
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User2Address
        })).TransactionResult;
        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.ShouldContain("Insufficient balance");
    }

    private async Task MultiTokenContract_Approve_Test()
    {
        await CreateAndIssueMultiTokensAsync();

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
        await CreateAndIssueMultiTokensAsync();

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
        await CreateTokenAndIssue();
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
        await CreateAndIssueMultiTokensAsync();

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
        await CreateAndIssueMultiTokensAsync();

        var allowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = User1Address,
            Symbol = AliceCoinTokenInfo.Symbol
        });

        allowanceOutput.Allowance.ShouldBe(0L);
        var result = (await TokenContractStub.UnApprove.SendAsync(new UnApproveInput
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
        var user1Stub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
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
                Symbol = AliceCoinTokenInfo.Symbol
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

    [Fact(DisplayName = "[MultiToken] Token transferFrom with different memo length.")]
    public async Task MultiTokenContract_TransferFrom_MemoLength_Test()
    {
        await MultiTokenContract_Approve_Test();
        var user1Stub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        {
            var result = await user1Stub.TransferFrom.SendAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultAddress,
                Memo = "MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest..",
                Symbol = AliceCoinTokenInfo.Symbol,
                To = User1Address
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var result = await user1Stub.TransferFrom.SendWithExceptionAsync(new TransferFromInput
            {
                Amount = 1000L,
                From = DefaultAddress,
                Memo = "MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest MemoTest...",
                Symbol = AliceCoinTokenInfo.Symbol,
                To = User1Address
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Invalid memo size.").ShouldBeTrue();
        }
    }

    [Fact(DisplayName = "[MultiToken] Address is in symbol whitelist.")]
    public async Task MultiTokenContract_TransferFrom_WhiteList_Test()
    {
        await CreateTokenAndIssue();
        var transferAmount = Amount.Div(3);
        var beforeTransferBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = TreasuryContractAddress,
            Symbol = SymbolForTest
        });
        beforeTransferBalance.Balance.ShouldBe(0);
        var allowance = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Spender = TreasuryContractAddress,
            Symbol = SymbolForTest,
            Owner = DefaultAddress
        });
        allowance.Allowance.ShouldBe(0);
        var isInSymbolWhitelist = await TokenContractStub.IsInWhiteList.CallAsync(new IsInWhiteListInput
        {
            Address = TreasuryContractAddress,
            Symbol = SymbolForTest
        });
        isInSymbolWhitelist.Value.ShouldBeTrue();
        var beforeTransferFromBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = SymbolForTest
        });
        await TreasuryContractStub.Donate.SendAsync(new DonateInput
        {
            Amount = transferAmount,
            Symbol = SymbolForTest
        });
        var afterTransferFromBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = SymbolForTest
        });
        afterTransferFromBalance.Balance.ShouldBe(beforeTransferFromBalance.Balance.Sub(transferAmount));
    }

    private async Task CreateTokenAndIssue(List<Address> whitelist = null, Address issueTo = null)
    {
        if (whitelist == null)
            whitelist = new List<Address>
            {
                BasicFunctionContractAddress,
                OtherBasicFunctionContractAddress,
                TreasuryContractAddress
            };
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
                whitelist
            }
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = SymbolForTest,
            Amount = DPoSContractConsts.LockTokenForElection * 200000,
            To = issueTo == null ? DefaultAddress : issueTo,
            Memo = "Issue"
        });
    }

    [Fact(DisplayName = "[MultiToken] Token lock and unlock test")]
    public async Task MultiTokenContract_LockAndUnLock_Test()
    {
        await CreateTokenAndIssue();

        var beforeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = SymbolForTest
        })).Balance;

        var lockId = HashHelper.ComputeFrom("lockId");

        // Lock.
        var lockTokenResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Amount = Amount,
            Symbol = SymbolForTest,
            LockId = lockId,
            Usage = "Testing."
        })).TransactionResult;
        lockTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var transferred = new Transferred();
        transferred.MergeFrom(lockTokenResult.Logs.First(l => l.Name == nameof(Transferred)));
        // Check balance of user after locking.
        {
            var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = SymbolForTest
            });
            result.Balance.ShouldBe(beforeBalance - Amount);
        }

        // Check locked amount
        {
            var amount = await BasicFunctionContractStub.GetLockedAmount.CallAsync(new GetLockedTokenAmountInput
            {
                Symbol = SymbolForTest,
                Address = DefaultAddress,
                LockId = lockId
            });
            amount.Amount.ShouldBe(Amount);
        }

        // Unlock.
        var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput
        {
            Address = DefaultAddress,
            Amount = Amount,
            Symbol = SymbolForTest,
            LockId = lockId,
            Usage = "Testing."
        })).TransactionResult;
        unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // Check balance of user after unlocking.
        {
            var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = SymbolForTest
            });
            result.Balance.ShouldBe(beforeBalance);
        }

        //Check amount of lock address after unlocking
        {
            var amount = await BasicFunctionContractStub.GetLockedAmount.CallAsync(new GetLockedTokenAmountInput
            {
                Symbol = SymbolForTest,
                Address = DefaultAddress,
                LockId = lockId
            });
            amount.Amount.ShouldBe(0);
        }
    }

    [Fact(DisplayName = "[MultiToken] Token lock through address not in whitelist")]
    public async Task MultiTokenContract_Lock_AddressNotInWhiteList_Test()
    {
        await CreateTokenAndIssue();

        // Try to lock.
        var lockId = HashHelper.ComputeFrom("lockId");
        var defaultSenderStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, DefaultKeyPair);
        // Lock.
        var lockResult = (await defaultSenderStub.Lock.SendWithExceptionAsync(new LockInput
        {
            Address = DefaultAddress,
            Amount = Amount,
            Symbol = SymbolForTest,
            LockId = lockId,
            Usage = "Testing."
        })).TransactionResult;

        lockResult.Status.ShouldBe(TransactionResultStatus.Failed);
        lockResult.Error.ShouldContain("No Permission.");
    }

    [Fact(DisplayName = "[MultiToken] When the allowance is sufficient, Token lock will deduct it")]
    public async Task Lock_With_Enough_Allowance_Test()
    {
        await CreateTokenAndIssue();
        var lockId = HashHelper.ComputeFrom("lockId");
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Amount = Amount,
            Symbol = SymbolForTest,
            Spender = BasicFunctionContractAddress
        });

        var allowanceBeforeLock = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Symbol = SymbolForTest,
            Spender = BasicFunctionContractAddress
        });
        allowanceBeforeLock.Allowance.ShouldBe(Amount);
        // Lock.
        var lockTokenResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Amount = Amount,
            Symbol = SymbolForTest,
            LockId = lockId,
            Usage = "Testing."
        })).TransactionResult;
        lockTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var allowanceAfterLock = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Symbol = SymbolForTest,
            Spender = BasicFunctionContractAddress
        });
        allowanceAfterLock.Allowance.ShouldBe(0);
    }

    [Fact(DisplayName = "[MultiToken] Token lock origin sender != input.Address")]
    public async Task MultiTokenContract_Lock_Invalid_Sender_Test()
    {
        await CreateTokenAndIssue();

        var lockId = HashHelper.ComputeFrom("lockId");

        // Lock.
        var lockTokenResult = (await BasicFunctionContractStub.LockToken.SendWithExceptionAsync(new LockTokenInput
        {
            Address = User2Address,
            Amount = Amount,
            Symbol = SymbolForTest,
            LockId = lockId,
            Usage = "Testing."
        })).TransactionResult;

        lockTokenResult.Status.ShouldBe(TransactionResultStatus.Failed);
        lockTokenResult.Error.ShouldContain("Lock behaviour should be initialed by origin address");
    }

    [Fact(DisplayName = "[MultiToken] Token lock with insufficient balance")]
    public async Task MultiTokenContract_Lock_WithInsufficientBalance_Test()
    {
        await CreateTokenAndIssue();

        var beforeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = SymbolForTest
        })).Balance;

        var lockId = HashHelper.ComputeFrom("lockId");
        // Lock.
        var lockResult = (await BasicFunctionContractStub.LockToken.SendWithExceptionAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Symbol = SymbolForTest,
            Amount = beforeBalance + 1,
            LockId = lockId,
            Usage = "Testing"
        })).TransactionResult;

        lockResult.Status.ShouldBe(TransactionResultStatus.Failed);
        lockResult.Error.ShouldContain("Insufficient balance");
    }

    /// <summary>
    ///     It's okay to unlock one locked token to get total amount via several times.
    /// </summary>
    /// <returns></returns>
    [Fact(DisplayName = "[MultiToken] Token unlock until no balance left")]
    public async Task MultiTokenContract_Unlock_repeatedly_Test()
    {
        await CreateTokenAndIssue();

        var lockId = HashHelper.ComputeFrom("lockId");

        // Lock.
        var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Symbol = SymbolForTest,
            Amount = Amount,
            LockId = lockId,
            Usage = "Testing"
        })).TransactionResult;
        lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // Unlock half of the amount at first.
        {
            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput
            {
                Address = DefaultAddress,
                Amount = Amount / 2,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // Unlock another half of the amount.
        {
            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendAsync(new UnlockTokenInput
            {
                Address = DefaultAddress,
                Amount = Amount / 2,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;

            unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // Cannot keep on unlocking.
        {
            var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(
                new UnlockTokenInput
                {
                    Address = DefaultAddress,
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
        await CreateTokenAndIssue();

        var lockId = HashHelper.ComputeFrom("lockId");

        // Lock.
        var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Symbol = SymbolForTest,
            Amount = Amount,
            LockId = lockId,
            Usage = "Testing"
        })).TransactionResult;
        lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(
            new UnlockTokenInput
            {
                Address = DefaultAddress,
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
        await CreateTokenAndIssue();

        var beforeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = SymbolForTest
        })).Balance;

        var lockId = HashHelper.ComputeFrom("lockId");

        // Lock.
        var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Symbol = SymbolForTest,
            Amount = Amount,
            LockId = lockId,
            Usage = "Testing"
        })).TransactionResult;
        lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // Check balance after locking.
        {
            var result = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = SymbolForTest
            });
            result.Balance.ShouldBe(beforeBalance - Amount);
        }

        var unlockResult = (await OtherBasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(
            new UnlockTokenInput
            {
                Address = DefaultAddress,
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
        await CreateTokenAndIssue();

        var lockId = HashHelper.ComputeFrom("lockId");

        // Lock.
        var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Symbol = SymbolForTest,
            Amount = Amount,
            LockId = lockId,
            Usage = "Testing"
        })).TransactionResult;
        lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(
            new UnlockTokenInput
            {
                Address = DefaultAddress,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = HashHelper.ComputeFrom("lockId1"),
                Usage = "Testing."
            })).TransactionResult;
        unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
        unlockResult.Error.ShouldContain("Insufficient balance");
    }

    [Fact(DisplayName = "[MultiToken] Unlock the token to another address that isn't the address locked")]
    public async Task MultiTokenContract_Unlock_ToOtherAddress_Test()
    {
        await CreateTokenAndIssue();

        var lockId = HashHelper.ComputeFrom("lockId");

        // Lock.
        var lockResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Symbol = SymbolForTest,
            Amount = Amount,
            LockId = lockId,
            Usage = "Testing"
        })).TransactionResult;
        lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var unlockResult = (await BasicFunctionContractStub.UnlockToken.SendWithExceptionAsync(
            new UnlockTokenInput
            {
                Address = User2Address,
                Amount = Amount,
                Symbol = SymbolForTest,
                LockId = lockId,
                Usage = "Testing."
            })).TransactionResult;
        unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
        unlockResult.Error.ShouldContain("Unlock behaviour should be initialed by origin address.");
    }

    [Fact(DisplayName = "[MultiToken] Token Burn Test")]
    public async Task MultiTokenContract_Burn_Test()
    {
        await CreateAndIssueMultiTokensAsync();
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

    [Fact(DisplayName = "[MultiToken] Token Burn invalid token")]
    public async Task MultiTokenContract_Burn_Invalid_Token_Test()
    {
        await CreateAndIssueMultiTokensAsync();
        var unburnedTokenSymbol = "UNBURNED";
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = unburnedTokenSymbol,
            TokenName = "Name",
            TotalSupply = 100_000_000_000L,
            Decimals = 10,
            IsBurnable = false,
            Issuer = DefaultAddress
        });
        var burnRet = await TokenContractStub.Burn.SendWithExceptionAsync(new BurnInput
        {
            Amount = 3000L,
            Symbol = unburnedTokenSymbol
        });
        burnRet.TransactionResult.Error.ShouldContain("The token is not burnable");
    }

    [Fact(DisplayName = "[MultiToken] Token Burn the amount greater than it's amount")]
    public async Task MultiTokenContract_Burn_BeyondBalance_Test()
    {
        await CreateAndIssueMultiTokensAsync();
        var user1Stub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        var result = (await user1Stub.Burn.SendWithExceptionAsync(new BurnInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            Amount = 3000L
        })).TransactionResult;
        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.ShouldContain("Insufficient balance");
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

    [Fact(DisplayName = "[MultiToken] invalid token symbol or amount")]
    public async Task TransferToContract_With_Invalid_Input_Test()
    {
        await CreateTokenAndIssue();
        var ret = await TokenContractStub.TransferToContract.SendWithExceptionAsync(new TransferToContractInput
        {
            Amount = -1,
            Symbol = SymbolForTest
        });
        ret.TransactionResult.Error.ShouldContain("Invalid amount");

        ret = await TokenContractStub.TransferToContract.SendWithExceptionAsync(new TransferToContractInput
        {
            Amount = 100,
            Symbol = "NOTEXIST"
        });
        ret.TransactionResult.Error.ShouldContain("Token is not found");
    }

    [Fact(DisplayName = "[MultiToken] sender is in whitelist, without approve, Token TransferToContract test")]
    public async Task TransferToContract_Out_Whitelist_Without_Approve_Test()
    {
        await CreateTokenAndIssue(new List<Address>());
        var transferAmount = Amount.Div(2);
        var transferResult = await BasicFunctionContractStub.TransferTokenToContract.SendWithExceptionAsync(
            new TransferTokenToContractInput
            {
                Amount = transferAmount,
                Symbol = SymbolForTest
            });
        transferResult.TransactionResult.Error.ShouldContain("Insufficient allowance");
    }

    [Fact(DisplayName = "[MultiToken] sender is in whitelist, without approve, Token TransferToContract test")]
    public async Task TransferToContract_In_Whitelist_Without_Approve_Test()
    {
        await CreateTokenAndIssue();
        var transferAmount = Amount.Div(2);
        var beforeBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = SymbolForTest,
            Owner = BasicFunctionContractAddress
        });
        await BasicFunctionContractStub.TransferTokenToContract.SendAsync(
            new TransferTokenToContractInput
            {
                Amount = transferAmount,
                Symbol = SymbolForTest
            });
        var afterBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = SymbolForTest,
            Owner = BasicFunctionContractAddress
        });
        afterBalance.Balance.ShouldBe(beforeBalance.Balance.Add(transferAmount));
    }

    [Fact(DisplayName = "[MultiToken] ChangeTokenIssuer test")]
    public async Task ChangeTokenIssuer_Test()
    {
        const string tokenSymbol = "PO";
        await CreateAndIssueMultiTokensAsync();
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = tokenSymbol,
            TokenName = "Name",
            TotalSupply = 100_000_000_000L,
            Decimals = 10,
            IsBurnable = true,
            Issuer = Accounts[1].Address
        });
        var tokenIssuerStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, Accounts[1].KeyPair);

        {
            var issueNotExistTokenRet = await tokenIssuerStub.ChangeTokenIssuer.SendWithExceptionAsync(
                new ChangeTokenIssuerInput
                {
                    Symbol = "NOTEXIST",
                    NewTokenIssuer = Accounts[2].Address
                });
            issueNotExistTokenRet.TransactionResult.Error.ShouldContain("invalid token symbol");
        }
        {
            await tokenIssuerStub.ChangeTokenIssuer.SendAsync(new ChangeTokenIssuerInput
            {
                Symbol = tokenSymbol,
                NewTokenIssuer = Accounts[2].Address
            });
            var tokenInfo = await tokenIssuerStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = tokenSymbol
            });
            tokenInfo.Issuer.ShouldBe(Accounts[2].Address);
        }
    }

    [Fact(DisplayName = "[MultiToken] sender is not the token issuer")]
    public async Task ChangeTokenIssuer_Without_Authorization_Test()
    {
        const string tokenSymbol = "PO";
        await CreateAndIssueMultiTokensAsync();
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = tokenSymbol,
            TokenName = "Name",
            TotalSupply = 100_000_000_000L,
            Decimals = 10,
            IsBurnable = true,
            Issuer = Accounts[1].Address
        });
        var changeIssuerRet = await TokenContractStub.ChangeTokenIssuer.SendWithExceptionAsync(
            new ChangeTokenIssuerInput
            {
                Symbol = tokenSymbol,
                NewTokenIssuer = Accounts[2].Address
            });
        changeIssuerRet.TransactionResult.Error.ShouldContain("permission denied");
    }

    [Fact(DisplayName = "[MultiToken] Token initialize from parent chain test")]
    public async Task InitializeFromParent_Test()
    {
        var netSymbol = "NET";
        var initializedFromParentRet =
            await TokenContractStub.InitializeFromParentChain.SendWithExceptionAsync(
                new InitializeFromParentChainInput());
        initializedFromParentRet.TransactionResult.Error.ShouldContain("creator should not be null");
        initializedFromParentRet =
            await TokenContractStub.InitializeFromParentChain.SendWithExceptionAsync(
                new InitializeFromParentChainInput
                {
                    Creator = DefaultAddress
                });
        initializedFromParentRet.TransactionResult.Error.ShouldContain("No permission");
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            defaultParliament, nameof(TokenContractStub.InitializeFromParentChain),
            new InitializeFromParentChainInput
            {
                Creator = DefaultAddress,
                ResourceAmount = { { netSymbol, 100 } },
                RegisteredOtherTokenContractAddresses = { { 1, ParliamentContractAddress } }
            });
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
        var resourceAmountDic = await TokenContractStub.GetResourceUsage.CallAsync(new Empty());
        resourceAmountDic.Value[netSymbol].ShouldBe(100);
        var chainWhitelist = await TokenContractStub.GetCrossChainTransferTokenContractAddress.CallAsync(
            new GetCrossChainTransferTokenContractAddressInput
            {
                ChainId = 1
            });
        chainWhitelist.ShouldBe(ParliamentContractAddress);
        initializedFromParentRet =
            await TokenContractStub.InitializeFromParentChain.SendWithExceptionAsync(
                new InitializeFromParentChainInput
                {
                    Creator = DefaultAddress
                });
        initializedFromParentRet.TransactionResult.Error.ShouldContain("MultiToken has been initialized");
    }

    [Fact(DisplayName = "[MultiToken] Side chain send create token")]
    public async Task Side_Chain_Creat_Token_Test()
    {
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            defaultParliament, nameof(TokenContractStub.InitializeFromParentChain),
            new InitializeFromParentChainInput
            {
                Creator = DefaultAddress
            });
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
        var createTokenRet = await TokenContractStub.Create.SendWithExceptionAsync(new CreateInput
        {
            Symbol = "ALI",
            TokenName = "Ali",
            Decimals = 4,
            TotalSupply = 100_000,
            Issuer = DefaultAddress
        });
        createTokenRet.TransactionResult.Error.ShouldContain(
            "Failed to create token if side chain creator already set.");
    }

    [Theory]
    [InlineData(10000, 1000, 0, 999, false, false)]
    [InlineData(10000, 1000, 0, 1001, false, true)]
    [InlineData(10000, 1000, 600, 599, true, false)]
    [InlineData(10000, 1000, 600, 601, true, true)]
    public async Task CheckThreshold_With_One_Token_Test(long totalSupply, long issueAmount, long ApproveAmount,
        long checkAmount, bool isCheckAllowance, bool isThrowException)
    {
        await CreateNativeTokenAsync();
        var tokenA = "AITA";
        await CreateAndIssueCustomizeTokenAsync(DefaultAddress, tokenA, totalSupply, issueAmount);
        if (ApproveAmount > 0)
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Amount = ApproveAmount,
                Spender = DefaultAddress,
                Symbol = tokenA
            });

        if (isThrowException)
        {
            var checkSufficientBalance = await TokenContractStub.CheckThreshold.SendWithExceptionAsync(
                new CheckThresholdInput
                {
                    IsCheckAllowance = isCheckAllowance,
                    Sender = DefaultAddress,
                    SymbolToThreshold = { { tokenA, checkAmount } }
                });
            checkSufficientBalance.TransactionResult.Error.ShouldContain("Cannot meet the calling threshold");
        }
        else
        {
            var checkSufficientBalance = await TokenContractStub.CheckThreshold.SendAsync(new CheckThresholdInput
            {
                IsCheckAllowance = isCheckAllowance,
                Sender = DefaultAddress,
                SymbolToThreshold = { { tokenA, checkAmount } }
            });
            checkSufficientBalance.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    [Theory]
    [InlineData(999, 0, 1000, 0, false, false)]
    [InlineData(1001, 0, 999, 0, false, false)]
    [InlineData(1001, 0, 1001, 0, false, true)]
    [InlineData(1001, 600, 1001, 600, true, true)]
    [InlineData(601, 600, 601, 600, true, true)]
    [InlineData(601, 600, 599, 600, true, false)]
    public async Task CheckThreshold_With_Multiple_Token_Test(long tokenACheckAmount, long tokenAApporveAmount,
        long tokenBCheckAmount, long tokenBApporveAmount, bool isCheckAllowance, bool isThrowException)
    {
        await CreateNativeTokenAsync();
        var tokenA = "AITA";
        await CreateAndIssueCustomizeTokenAsync(DefaultAddress, tokenA, 10000, 1000);
        var tokenB = "AITB";
        await CreateAndIssueCustomizeTokenAsync(DefaultAddress, tokenB, 10000, 1000);
        if (tokenAApporveAmount > 0)
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Amount = tokenAApporveAmount,
                Spender = DefaultAddress,
                Symbol = tokenA
            });

        if (tokenBApporveAmount > 0)
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Amount = tokenBApporveAmount,
                Spender = DefaultAddress,
                Symbol = tokenB
            });

        if (isThrowException)
        {
            var checkSufficientBalance = await TokenContractStub.CheckThreshold.SendWithExceptionAsync(
                new CheckThresholdInput
                {
                    IsCheckAllowance = isCheckAllowance,
                    Sender = DefaultAddress,
                    SymbolToThreshold = { { tokenA, tokenACheckAmount }, { tokenB, tokenBCheckAmount } }
                });
            checkSufficientBalance.TransactionResult.Error.ShouldContain("Cannot meet the calling threshold");
        }
        else
        {
            var checkSufficientBalance = await TokenContractStub.CheckThreshold.SendAsync(new CheckThresholdInput
            {
                IsCheckAllowance = isCheckAllowance,
                Sender = DefaultAddress,
                SymbolToThreshold = { { tokenA, tokenACheckAmount }, { tokenB, tokenBCheckAmount } }
            });
            checkSufficientBalance.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    private async Task CreateAndIssueCustomizeTokenAsync(Address creator, string symbol, long totalSupply,
        long issueAmount,
        Address to = null, params string[] otherParameters)
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = symbol,
            Issuer = creator,
            TokenName = symbol + "name",
            TotalSupply = totalSupply,
            Decimals = 4
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = symbol,
            Amount = issueAmount,
            To = to == null ? creator : to
        });
    }

    [Fact]
    public async Task ValidateTokenInfoExists_ExternalInfo_Test()
    {
        await CreateNativeTokenAsync();
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            TokenName = AliceCoinTokenInfo.TokenName,
            TotalSupply = AliceCoinTokenInfo.TotalSupply,
            Decimals = AliceCoinTokenInfo.Decimals,
            Issuer = AliceCoinTokenInfo.Issuer,
            IsBurnable = AliceCoinTokenInfo.IsBurnable,
            LockWhiteList =
            {
                BasicFunctionContractAddress,
                OtherBasicFunctionContractAddress,
                TokenConverterContractAddress,
                TreasuryContractAddress
            }
        });

        var result = await TokenContractStub.ValidateTokenInfoExists.SendAsync(
            new ValidateTokenInfoExistsInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                TokenName = AliceCoinTokenInfo.TokenName,
                TotalSupply = AliceCoinTokenInfo.TotalSupply,
                Decimals = AliceCoinTokenInfo.Decimals,
                Issuer = AliceCoinTokenInfo.Issuer,
                IsBurnable = AliceCoinTokenInfo.IsBurnable,
                IssueChainId = _chainId
            });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        result = await TokenContractStub.ValidateTokenInfoExists.SendWithExceptionAsync(
            new ValidateTokenInfoExistsInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                TokenName = AliceCoinTokenInfo.TokenName,
                TotalSupply = AliceCoinTokenInfo.TotalSupply,
                Decimals = AliceCoinTokenInfo.Decimals,
                Issuer = AliceCoinTokenInfo.Issuer,
                IsBurnable = AliceCoinTokenInfo.IsBurnable,
                ExternalInfo = { { "key", "value" } }
            });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
    }

    [Fact]
    public async Task CrossContractCreateToken_Test()
    {
        await CreateNativeTokenAsync();
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = NativeToken,
            To = BasicFunctionContractAddress,
            Amount = 10000_00000000
        });

        var fee = await TokenContractStub.GetMethodFee.CallAsync(new StringValue { Value = "Create" });
        var createTokenInput = new CreateTokenThroughMultiTokenInput
        {
            Symbol = "TEST",
            Decimals = 8,
            TokenName = "TEST token",
            Issuer = DefaultAddress,
            IsBurnable = true,
            TotalSupply = TotalSupply,
            ExternalInfo = new TestContract.BasicFunction.ExternalInfo()
        };
        var result = await BasicFunctionContractStub.CreateTokenThroughMultiToken.SendAsync(createTokenInput);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var logs = result.TransactionResult.Logs.First(l => l.Name.Equals("TransactionFeeCharged"));
        var transactionFeeCharged = TransactionFeeCharged.Parser.ParseFrom(logs.NonIndexed);
        transactionFeeCharged.Amount.ShouldBe(fee.Fees.First().BasicFee);
        transactionFeeCharged.Symbol.ShouldBe(fee.Fees.First().Symbol);

        var tokenBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            { Owner = TokenContractAddress, Symbol = NativeToken });
        tokenBalance.Balance.ShouldBe(0);
        
        var functionBalance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            { Owner = BasicFunctionContractAddress, Symbol = NativeToken });
        functionBalance.Balance.ShouldBe(0);

        var checkTokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput { Symbol = "TEST" });
        checkTokenInfo.Decimals.ShouldBe(createTokenInput.Decimals);
        checkTokenInfo.Issuer.ShouldBe(createTokenInput.Issuer);
        checkTokenInfo.Decimals.ShouldBe(createTokenInput.Decimals);
        checkTokenInfo.TokenName.ShouldBe(createTokenInput.TokenName);
        checkTokenInfo.TotalSupply.ShouldBe(createTokenInput.TotalSupply);
        checkTokenInfo.IsBurnable.ShouldBe(createTokenInput.IsBurnable);
        checkTokenInfo.ExternalInfo.Value.ShouldBe(createTokenInput.ExternalInfo.Value);
    }
}