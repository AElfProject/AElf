using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS10;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using AElf.Contracts.Association;
using AElf.ContractTestBase.ContractTestKit;
using Google.Protobuf;
using AElf.Standards.ACS3;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
{
    protected const string SymbolForTest = "GHJ";
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

    [Fact(DisplayName = "[MultiToken] BatchApprove token to Contract")]
    public async Task MultiTokenContract_BatchApprove_ContractAddress_Test()
    {
        await CreateTokenAndIssue();
        var approveBasisResult = (await TokenContractStub.BatchApprove.SendAsync(new BatchApproveInput
        {
            Value =
            {
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 2000L,
                    Spender = BasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 1000L,
                    Spender = OtherBasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 5000L,
                    Spender = TreasuryContractAddress
                }
            }
        })).TransactionResult;
        approveBasisResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var basicAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = BasicFunctionContractAddress,
            Symbol = SymbolForTest
        });
        basicAllowanceOutput.Allowance.ShouldBe(2000L);
        var otherBasicAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = OtherBasicFunctionContractAddress,
            Symbol = SymbolForTest
        });
        otherBasicAllowanceOutput.Allowance.ShouldBe(1000L);
        var treasuryAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = TreasuryContractAddress,
            Symbol = SymbolForTest
        });
        treasuryAllowanceOutput.Allowance.ShouldBe(5000L);

        approveBasisResult = (await TokenContractStub.BatchApprove.SendAsync(new BatchApproveInput
        {
            Value =
            {
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 1000L,
                    Spender = BasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 3000L,
                    Spender = BasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 3000L,
                    Spender = TreasuryContractAddress
                }
            }
        })).TransactionResult;
        approveBasisResult.Status.ShouldBe(TransactionResultStatus.Mined);
        basicAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = BasicFunctionContractAddress,
            Symbol = SymbolForTest
        });
        basicAllowanceOutput.Allowance.ShouldBe(3000L);

        treasuryAllowanceOutput = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = TreasuryContractAddress,
            Symbol = SymbolForTest
        });
        treasuryAllowanceOutput.Allowance.ShouldBe(3000L);
    }

    [Fact]
    public async Task MultiTokenContract_SetMaximumBatchApproveCount_Test()
    {
        var result = await TokenContractStub.SetMaxBatchApproveCount.SendWithExceptionAsync(new Int32Value
        {
            Value = 1
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.ShouldContain("Unauthorized behavior");
        var maximumBatchApproveCountOutput = await TokenContractStub.GetMaxBatchApproveCount.CallAsync(new Empty());
        maximumBatchApproveCountOutput.Value.ShouldBe(100);
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            defaultParliament, nameof(TokenContractStub.SetMaxBatchApproveCount),
            new Int32Value
            {
                Value = 1
            });
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
        maximumBatchApproveCountOutput = await TokenContractStub.GetMaxBatchApproveCount.CallAsync(new Empty());
        maximumBatchApproveCountOutput.Value.ShouldBe(1);
        await CreateTokenAndIssue();
        var approveBasisResult = (await TokenContractStub.BatchApprove.SendWithExceptionAsync(new BatchApproveInput
        {
            Value =
            {
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 2000L,
                    Spender = BasicFunctionContractAddress
                },
                new ApproveInput
                {
                    Symbol = SymbolForTest,
                    Amount = 1000L,
                    Spender = OtherBasicFunctionContractAddress
                }
            }
        })).TransactionResult;
        approveBasisResult.Status.ShouldBe(TransactionResultStatus.Failed);
        approveBasisResult.Error.ShouldContain("Exceeds the max batch approve count");
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

    private async Task CreateNft()
    {
        await CreateMutiTokenAsync(TokenContractStub, new CreateInput
        {
            TokenName = "Test",
            TotalSupply = TotalSupply,
            Decimals = 0,
            Issuer = DefaultAddress,
            Owner = DefaultAddress,
            IssueChainId = _chainId,
            Symbol = "ABC-0"
        });
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            TokenName = "Test",
            TotalSupply = TotalSupply,
            Decimals = 0,
            Issuer = DefaultAddress,
            Owner = DefaultAddress,
            IssueChainId = _chainId,
            Symbol = "ABC-1"
        });
    }

    [Fact]
    public async Task MultiTokenContract_TransferFrom_Nft_Global_Test()
    {
        await CreateNft();
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ABC-1",
            Amount = 100,
            To = DefaultAddress,
            Memo = "test"
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ABC-1",
            Amount = 200,
            To = User1Address,
            Memo = "test"
        });
        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ABC-1"
        });
        balance.Balance.ShouldBe(100);
        balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = User1Address,
            Symbol = "ABC-1"
        });
        balance.Balance.ShouldBe(200);

        {
            var executionResult = await TokenContractStub.Approve.SendWithExceptionAsync(new ApproveInput
            {
                Amount = 1000,
                Symbol = "*",
                Spender = User1Address
            });
            executionResult.TransactionResult.Error.ShouldContain("Token is not found.");
        }

        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Amount = 1,
            Symbol = "ABC-*",
            Spender = User1Address
        });

        await CheckAllowance("ABC-1", 0);
        await CheckAllowance("ELF", 0);
        await CheckAvailableAllowance("ABC-1", 1);
        await CheckAvailableAllowance("ELF", 0);

        var user1Stub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);

        {
            var executionResult = await user1Stub.TransferFrom.SendWithExceptionAsync(new TransferFromInput
            {
                Amount = 50,
                From = DefaultAddress,
                Memo = "test",
                Symbol = "ABC-1",
                To = User1Address
            });
            executionResult.TransactionResult.Error.ShouldContain("Insufficient allowance.");
        }

        await CheckAllowance("ABC-1", 0);
        await CheckAvailableAllowance("ABC-1", 1);

        // Not changed actually.
        balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "ABC-1"
        });
        balance.Balance.ShouldBe(100);
        balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = User1Address,
            Symbol = "ABC-1"
        });
        balance.Balance.ShouldBe(200);
    }

    private async Task CheckAllowance(string symbol, long shouldBeAllowance)
    {
        var allowance = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = User1Address,
            Symbol = symbol
        });
        allowance.Allowance.ShouldBe(shouldBeAllowance);
    }

    private async Task CheckAvailableAllowance(string symbol, long shouldBeAllowance)
    {
        var allowance = await TokenContractStub.GetAvailableAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = User1Address,
            Symbol = symbol
        });
        allowance.Allowance.ShouldBe(shouldBeAllowance);
    }

    [Fact]
    public async Task MultiTokenContract_TransferFrom_Nft_Collection_Test()
    {
        await CreateNft();
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ABC-1",
            Amount = 100,
            To = DefaultAddress,
            Memo = "test"
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ABC-1",
            Amount = 200,
            To = User1Address,
            Memo = "test"
        });

        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Amount = 1000,
            Symbol = "ABC-*",
            Spender = User1Address
        });

        await CheckAllowance("ABC-1", 0);
        await CheckAvailableAllowance("ABC-1", 1000);
        await CheckAvailableAllowance("ELF", 0);

        var user1Stub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        var result2 = await user1Stub.TransferFrom.SendAsync(new TransferFromInput
        {
            Amount = 50,
            From = DefaultAddress,
            Memo = "test",
            Symbol = "ABC-1",
            To = User1Address
        }); 
        result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        await CheckAvailableAllowance("ABC-1", 1000 - 50);
    }

    [Fact]
    public async Task MultiTokenContract_TransferFrom_Token_Test()
    {
        await CreateAndIssueToken();
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Amount = 100_00000000,
            Symbol = "SSS",
            Spender = User1Address
        });
        await CheckAllowance("SSS", 100_00000000);
        await CheckAvailableAllowance("SSS", 100_00000000);
        await CheckAllowance("ELF", 0);

        var user1Stub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        var result2 = await user1Stub.TransferFrom.SendAsync(new TransferFromInput
        {
            Amount = 50_00000000,
            From = DefaultAddress,
            Memo = "test",
            Symbol = "SSS",
            To = User1Address
        });
        result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        await CheckAvailableAllowance("SSS", 100_00000000 - 50_00000000);

        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "SSS"
        });
        balance.Balance.ShouldBe(TotalSupply - 50_00000000);
        balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = User1Address,
            Symbol = "SSS"
        });
        balance.Balance.ShouldBe(50_00000000);
    }

    private async Task CreateAndIssueToken()
    {
        await CreateMutiTokenAsync(TokenContractStub, new CreateInput
        {
            TokenName = "Test",
            TotalSupply = TotalSupply,
            Decimals = 8,
            Issuer = DefaultAddress,
            Owner = DefaultAddress,
            IssueChainId = _chainId,
            Symbol = "SSS"
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "SSS",
            Amount = TotalSupply,
            To = DefaultAddress,
            Memo = "Issue"
        });
        var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = DefaultAddress,
            Symbol = "SSS"
        });
        balance.Balance.ShouldBe(TotalSupply);
    }

    [Fact]
    public async Task MultiTokenContract_Approve_Test_New()
    {
        await CreateAndIssueToken();
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = User1Address,
            Symbol = "SSS",
            Amount = 100_000000000
        });
        var allowance = await TokenContractStub.GetAllowance.CallAsync(new GetAllowanceInput
        {
            Owner = DefaultAddress,
            Spender = User1Address,
            Symbol = "SSS"
        });
        allowance.Allowance.ShouldBe(100_000000000);

        await CheckAllowance("SSS", 100_000000000);
        await CheckAvailableAllowance("SSS", 100_000000000);

        await TokenContractStub.UnApprove.SendAsync(new UnApproveInput
        {
            Spender = User1Address,
            Symbol = "SSS",
            Amount = 20_000000000
        });
        await CheckAvailableAllowance("SSS", 100_000000000 - 20_000000000);
    }

    [Fact]
    public async Task MultiTokenContract_Approve_Test_New_Fail()
    {
        await CreateAndIssueToken();
        {
            var executionResult = await TokenContractStub.Approve.SendWithExceptionAsync(new ApproveInput
            {
                Spender = User1Address,
                Symbol = "SSS*",
                Amount = 100_000000000
            });
            executionResult.TransactionResult.Error.ShouldContain("Token is not found.");
        }
        {
            var executionResult = await TokenContractStub.Approve.SendWithExceptionAsync(new ApproveInput
            {
                Spender = User1Address,
                Symbol = "SSS**",
                Amount = 100_000000000
            });
            executionResult.TransactionResult.Error.ShouldContain("Token is not found.");
        }
        {
            var executionResult = await TokenContractStub.Approve.SendWithExceptionAsync(new ApproveInput
            {
                Spender = User1Address,
                Symbol = "*-*",
                Amount = 100_000000000
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid symbol.");
        }
    }
    
    [Fact]
    public async Task MultiTokenContract_Approve_Test_New_Nft_Fail()
    {
        await CreateNft();
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = "ABC-1",
            Amount = 100,
            To = DefaultAddress,
            Memo = "test"
        });
        {
            var executionResult = await TokenContractStub.Approve.SendWithExceptionAsync(new ApproveInput
            {
                Spender = User1Address,
                Symbol = "AB*-*",
                Amount = 100_000000000
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid Symbol");
        }
        {
            var executionResult = await TokenContractStub.Approve.SendWithExceptionAsync(new ApproveInput
            {
                Spender = User1Address,
                Symbol = "ABC-*9",
                Amount = 100_000000000
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid NFT Symbol.");
        }
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
        await CreateMutiTokenAsync(TokenContractStub,new CreateInput
        {
            Symbol = SymbolForTest,
            Decimals = 2,
            IsBurnable = true,
            Issuer = DefaultAddress,
            Owner = DefaultAddress,
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
        await CreateMutiTokenAsync(TokenContractStub, new CreateInput
        {
            Symbol = unburnedTokenSymbol,
            TokenName = "Name",
            TotalSupply = 100_000_000_000L,
            Decimals = 10,
            IsBurnable = false,
            Issuer = DefaultAddress,
            Owner = DefaultAddress,
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

        proposalId = await CreateProposalAsync(TokenContractAddress,
            defaultParliament, nameof(TokenContractStub.Create),
            new CreateInput
            {
                Symbol = "ALI",
                TokenName = "Ali",
                Decimals = 4,
                TotalSupply = 100_000,
                Issuer = DefaultAddress,
                Owner = DefaultAddress
            });
        await ApproveWithMinersAsync(proposalId);
        var createTokenRe = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        createTokenRe.TransactionResult.Error.ShouldContain(
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
        await CreateMutiTokenAsync(TokenContractStub,new CreateInput
        {
            Symbol = symbol,
            Issuer = creator,
            Owner = creator,
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
        await CreateMutiTokenAsync(TokenContractStub, new CreateInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            TokenName = AliceCoinTokenInfo.TokenName,
            TotalSupply = AliceCoinTokenInfo.TotalSupply,
            Decimals = AliceCoinTokenInfo.Decimals,
            Issuer = AliceCoinTokenInfo.Issuer,
            Owner = AliceCoinTokenInfo.Issuer,
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
                Owner = AliceCoinTokenInfo.Issuer,
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
                Owner = AliceCoinTokenInfo.Issuer,
                IsBurnable = AliceCoinTokenInfo.IsBurnable,
                ExternalInfo = { { "key", "value" } }
            });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
    }

    [Fact]
    public async Task CrossContractCreateToken_Test()
    {
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
        var input = new CreateInput
        {
            Symbol = SeedNFTSymbolPre + 100,
            Decimals = 0,
            IsBurnable = true,
            TokenName = "seed token" + 100,
            TotalSupply = 1,
            Issuer = DefaultAddress,
            ExternalInfo = new ExternalInfo(),
            LockWhiteList = { TokenContractAddress },
            Owner = DefaultAddress
        };
        input.ExternalInfo.Value["__seed_owned_symbol"] = createTokenInput.Symbol;
        input.ExternalInfo.Value["__seed_exp_time"] = TimestampHelper.GetUtcNow().AddDays(1).Seconds.ToString();
        await TokenContractStub.Create.SendAsync(input);
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = input.Symbol,
            Amount = 1,
            Memo = "ddd",
            To = BasicFunctionContractAddress
        });

        var result = await BasicFunctionContractStub.CreateTokenThroughMultiToken.SendAsync(createTokenInput);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var checkTokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput { Symbol = "TEST" });
        checkTokenInfo.Decimals.ShouldBe(createTokenInput.Decimals);
        checkTokenInfo.Issuer.ShouldBe(createTokenInput.Issuer);
        checkTokenInfo.Decimals.ShouldBe(createTokenInput.Decimals);
        checkTokenInfo.TokenName.ShouldBe(createTokenInput.TokenName);
        checkTokenInfo.TotalSupply.ShouldBe(createTokenInput.TotalSupply);
        checkTokenInfo.IsBurnable.ShouldBe(createTokenInput.IsBurnable);
        checkTokenInfo.ExternalInfo.Value.ShouldBe(createTokenInput.ExternalInfo.Value);
    }

    [Fact]
    public async Task TokenIssuerAndOwnerModification_Test()
    {
        var result = await TokenContractStub.ModifyTokenIssuerAndOwner.SendWithExceptionAsync(new ModifyTokenIssuerAndOwnerInput());
        result.TransactionResult.Error.ShouldContain("Invalid input symbol.");
        
        result = await TokenContractStub.ModifyTokenIssuerAndOwner.SendWithExceptionAsync(new ModifyTokenIssuerAndOwnerInput
        {
            Symbol = "TEST"
        });
        result.TransactionResult.Error.ShouldContain("Invalid input issuer.");
        
        result = await TokenContractStub.ModifyTokenIssuerAndOwner.SendWithExceptionAsync(new ModifyTokenIssuerAndOwnerInput
        {
            Symbol = "TEST",
            Issuer = DefaultAddress
        });
        result.TransactionResult.Error.ShouldContain("Invalid input owner.");
        
        result = await TokenContractStub.ModifyTokenIssuerAndOwner.SendWithExceptionAsync(new ModifyTokenIssuerAndOwnerInput
        {
            Symbol = "TEST",
            Issuer = DefaultAddress,
            Owner = DefaultAddress
        });
        result.TransactionResult.Error.ShouldContain("Token is not found.");
        
        result = await TokenContractStubUser.ModifyTokenIssuerAndOwner.SendWithExceptionAsync(new ModifyTokenIssuerAndOwnerInput
        {
            Symbol = DefaultSymbol,
            Issuer = DefaultAddress,
            Owner = DefaultAddress
        });
        result.TransactionResult.Error.ShouldContain("Only token issuer can set token issuer and owner.");
        
        result = await TokenContractStub.ModifyTokenIssuerAndOwner.SendWithExceptionAsync(new ModifyTokenIssuerAndOwnerInput
        {
            Symbol = DefaultSymbol,
            Issuer = DefaultAddress,
            Owner = DefaultAddress
        });
        result.TransactionResult.Error.ShouldContain("Can only set token which does not have owner.");
        
        var output = await TokenContractStub.GetTokenIssuerAndOwnerModificationEnabled.CallAsync(new Empty());
        output.Value.ShouldBeTrue();
        
        result = await TokenContractStub.SetTokenIssuerAndOwnerModificationEnabled.SendWithExceptionAsync(
            new SetTokenIssuerAndOwnerModificationEnabledInput
            {
                Enabled = false
            });
        result.TransactionResult.Error.ShouldContain("Unauthorized behavior.");
        
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            defaultParliament, nameof(TokenContractStub.SetTokenIssuerAndOwnerModificationEnabled),
            new SetTokenIssuerAndOwnerModificationEnabledInput
            {
                Enabled = false
            });
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
        
        output = await TokenContractStub.GetTokenIssuerAndOwnerModificationEnabled.CallAsync(new Empty());
        output.Value.ShouldBeFalse();
        
        result = await TokenContractStub.ModifyTokenIssuerAndOwner.SendWithExceptionAsync(new ModifyTokenIssuerAndOwnerInput
        {
            Symbol = DefaultSymbol,
            Issuer = DefaultAddress,
            Owner = DefaultAddress
        });
        result.TransactionResult.Error.ShouldContain("Set token issuer and owner disabled.");

    }
    
    [Theory]
    [InlineData("SEED-0", 1731927992000)]
    public async Task ExtendSeedExpirationTime_Test(string symbol, long expirationTime)
    {
        ExtendSeedExpirationTimeInput input = new ExtendSeedExpirationTimeInput();
        input.Symbol = symbol;
        input.ExpirationTime = expirationTime;

        await TokenContractStub.ExtendSeedExpirationTime.CallAsync(input);
    }

    [Fact]
    public async Task MultiTokenContract_Transfer_BlackList_Test()
    {
        await MultiTokenContract_Approve_Test();
        
        var trafficToken = "TRAFFIC";
        await CreateAndIssueCustomizeTokenAsync(DefaultAddress, trafficToken, 10000, 10000);

        // Non-owner cannot add to blacklist
        var addBlackListResult = await TokenContractStubUser.AddToTransferBlackList.SendWithExceptionAsync(DefaultAddress);
        addBlackListResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        addBlackListResult.TransactionResult.Error.ShouldContain("No permission");
        var isInTransferBlackList = await TokenContractStubUser.IsInTransferBlackList.CallAsync(DefaultAddress);
        isInTransferBlackList.Value.ShouldBe(false);

        // Owner adds DefaultAddress to blacklist via parliament proposal
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, nameof(TokenContractStub.AddToTransferBlackList), DefaultAddress);
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
        isInTransferBlackList = await TokenContractStubUser.IsInTransferBlackList.CallAsync(DefaultAddress);
        isInTransferBlackList.Value.ShouldBe(true);

        // Transfer should fail when sender is in blacklist
        var transferResult = (await TokenContractStub.Transfer.SendWithExceptionAsync(new TransferInput
        {
            Amount = Amount,
            Memo = "blacklist test",
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        })).TransactionResult;
        transferResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transferResult.Error.ShouldContain("From address is in transfer blacklist");

        // TransferFrom should fail when from address is in blacklist
        var user1Stub = GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        var transferFromResult = (await user1Stub.TransferFrom.SendWithExceptionAsync(new TransferFromInput
        {
            Amount = Amount,
            From = DefaultAddress,
            Memo = "blacklist test",
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        })).TransactionResult;
        transferFromResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transferFromResult.Error.ShouldContain("From address is in transfer blacklist");

        // CrossChainTransfer should fail when sender is in blacklist
        var crossChainTransferResult = (await TokenContractStub.CrossChainTransfer.SendWithExceptionAsync(new CrossChainTransferInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            Amount = Amount,
            To = User1Address,
            IssueChainId = 9992731,
            Memo = "blacklist test",
            ToChainId = 9992732
        })).TransactionResult;
        crossChainTransferResult.Status.ShouldBe(TransactionResultStatus.Failed);
        crossChainTransferResult.Error.ShouldContain("Sender is in transfer blacklist");
        
        // Lock should fail when sender is in blacklist
        var lockId = HashHelper.ComputeFrom("lockId");
        var lockTokenResult = (await BasicFunctionContractStub.LockToken.SendWithExceptionAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            LockId = lockId,
            Usage = "Testing."
        })).TransactionResult;
        lockTokenResult.Status.ShouldBe(TransactionResultStatus.Failed);
        lockTokenResult.Error.ShouldContain("From address is in transfer blacklist");

        // Transfer to contract should fail when sender is in blacklist
        var transferToContractResult = (await BasicFunctionContractStub.TransferTokenToContract.SendWithExceptionAsync(
            new TransferTokenToContractInput
            {
                Amount = Amount,
                Symbol = AliceCoinTokenInfo.Symbol
            })).TransactionResult;
        transferToContractResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transferToContractResult.Error.ShouldContain("From address is in transfer blacklist");
        
        // AdvanceResourceToken should fail when sender is in blacklist
        var advanceRet = await TokenContractStub.AdvanceResourceToken.SendWithExceptionAsync(
            new AdvanceResourceTokenInput
            {
                ContractAddress = BasicFunctionContractAddress,
                Amount = Amount,
                ResourceTokenSymbol = trafficToken
            });
        advanceRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        advanceRet.TransactionResult.Error.ShouldContain("From address is in transfer blacklist");

        // Non-owner cannot remove from blacklist
        var removeBlackListResult = await TokenContractStubUser.RemoveFromTransferBlackList.SendWithExceptionAsync(DefaultAddress);
        removeBlackListResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        removeBlackListResult.TransactionResult.Error.ShouldContain("Unauthorized behavior");

        // Owner removes DefaultAddress from blacklist via parliament proposal
        var removeProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, nameof(TokenContractStub.RemoveFromTransferBlackList), DefaultAddress);
        await ApproveWithMinersAsync(removeProposalId);
        await ParliamentContractStub.Release.SendAsync(removeProposalId);
        isInTransferBlackList = await TokenContractStubUser.IsInTransferBlackList.CallAsync(DefaultAddress);
        isInTransferBlackList.Value.ShouldBe(false);

        // Transfer should succeed after removing from blacklist
        var transferResult2 = await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Memo = "blacklist test",
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        transferResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        // TransferFrom should succeed after removing from blacklist
        transferFromResult = (await user1Stub.TransferFrom.SendAsync(new TransferFromInput
        {
            Amount = Amount,
            From = DefaultAddress,
            Memo = "blacklist test",
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        })).TransactionResult;
        transferFromResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // CrossChainTransfer should succeed after removing from blacklist
        crossChainTransferResult = (await TokenContractStub.CrossChainTransfer.SendAsync(new CrossChainTransferInput
        {
            Symbol = AliceCoinTokenInfo.Symbol,
            Amount = Amount,
            To = User1Address,
            IssueChainId = 9992731,
            Memo = "blacklist test",
            ToChainId = 9992732
        })).TransactionResult;
        crossChainTransferResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        // Lock should succeed after removing from blacklist
        lockTokenResult = (await BasicFunctionContractStub.LockToken.SendAsync(new LockTokenInput
        {
            Address = DefaultAddress,
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            LockId = lockId,
            Usage = "Testing."
        })).TransactionResult;
        lockTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        // Transfer to contract should succeed after removing from blacklist
        transferToContractResult = (await BasicFunctionContractStub.TransferTokenToContract.SendAsync(new TransferTokenToContractInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol
        })).TransactionResult;
        transferToContractResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        // AdvanceResourceToken should succeed after removing from blacklist
        advanceRet = await TokenContractStub.AdvanceResourceToken.SendAsync(
            new AdvanceResourceTokenInput
            {
                ContractAddress = BasicFunctionContractAddress,
                Amount = Amount,
                ResourceTokenSymbol = trafficToken
            });
        advanceRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        // Test initial TransferBlackListController should fallback to Parliament
        var initialController = await TokenContractStub.GetTransferBlackListController.CallAsync(new Empty());
        initialController.OwnerAddress.ShouldBe(defaultParliament);
        
        // Create Association organization for TransferBlackListController
        var associationStub = GetTester<AssociationContractImplContainer.AssociationContractImplStub>(AssociationContractAddress, DefaultKeyPair);
        var organizationCreated = await associationStub.CreateOrganization.SendAsync(new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = 1,
                MinimalVoteThreshold = 1,
                MaximalAbstentionThreshold = 0,
                MaximalRejectionThreshold = 0
            },
            ProposerWhiteList = new ProposerWhiteList
            {
                Proposers = { DefaultAddress }
            },
            OrganizationMemberList = new OrganizationMemberList
            {
                OrganizationMembers = { DefaultAddress }
            }
        });
        organizationCreated.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var organizationAddress = Address.Parser.ParseFrom(organizationCreated.TransactionResult.ReturnValue);
        
        // Only Parliament can change TransferBlackListController
        var changeControllerResult = await TokenContractStubUser.ChangeTransferBlackListController.SendWithExceptionAsync(new AuthorityInfo
        {
            ContractAddress = AssociationContractAddress,
            OwnerAddress = organizationAddress
        });
        changeControllerResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        changeControllerResult.TransactionResult.Error.ShouldContain("Unauthorized behavior");
        
        // Test setting non-existent association organization address should fail
        var nonExistentOrgAddress = SampleAddress.AddressList[9]; // Use a non-existent organization address
        var setNonExistentControllerProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.ChangeTransferBlackListController), new AuthorityInfo
            {
                ContractAddress = AssociationContractAddress,
                OwnerAddress = nonExistentOrgAddress
            });
        await ApproveWithMinersAsync(setNonExistentControllerProposalId);
        var setNonExistentControllerResult = await ParliamentContractStub.Release.SendWithExceptionAsync(setNonExistentControllerProposalId);
        setNonExistentControllerResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        setNonExistentControllerResult.TransactionResult.Error.ShouldContain("Invalid authority input");
        
        // Parliament changes TransferBlackListController to Association organization
        var changeControllerProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.ChangeTransferBlackListController), new AuthorityInfo
            {
                ContractAddress = AssociationContractAddress,
                OwnerAddress = organizationAddress
            });
        await ApproveWithMinersAsync(changeControllerProposalId);
        await ParliamentContractStub.Release.SendAsync(changeControllerProposalId);
        
        // Verify TransferBlackListController has been changed
        var newController = await TokenContractStub.GetTransferBlackListController.CallAsync(new Empty());
        newController.ContractAddress.ShouldBe(AssociationContractAddress);
        newController.OwnerAddress.ShouldBe(organizationAddress);
        
        // Association organization can now add addresses to blacklist directly
        var addToBlackListViaAssociation = await associationStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContractStub.AddToTransferBlackList),
            ToAddress = TokenContractAddress,
            Params = User2Address.ToByteString(),
            OrganizationAddress = organizationAddress,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        });
        addToBlackListViaAssociation.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var blacklistProposalId = ProposalCreated.Parser.ParseFrom(addToBlackListViaAssociation.TransactionResult.Logs
            .First(l => l.Name == nameof(ProposalCreated)).NonIndexed).ProposalId;
        
        // Approve and release the proposal
        await associationStub.Approve.SendAsync(blacklistProposalId);
        await associationStub.Release.SendAsync(blacklistProposalId);
        
        // Verify User2Address is now in blacklist
        var isUser2InBlackList = await TokenContractStub.IsInTransferBlackList.CallAsync(User2Address);
        isUser2InBlackList.Value.ShouldBe(true);
        
        // User2 transfer should fail when in blacklist
        var user2Stub = GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User2KeyPair);
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User2Address
        });
        var user2TransferResult = await user2Stub.Transfer.SendWithExceptionAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        user2TransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        user2TransferResult.TransactionResult.Error.ShouldContain("From address is in transfer blacklist");
        
        // Parliament can still remove from blacklist (not affected by controller change)
        var removeUser2ProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.RemoveFromTransferBlackList), User2Address);
        await ApproveWithMinersAsync(removeUser2ProposalId);
        await ParliamentContractStub.Release.SendAsync(removeUser2ProposalId);
        
        // Verify User2Address is removed from blacklist
        isUser2InBlackList = await TokenContractStub.IsInTransferBlackList.CallAsync(User2Address);
        isUser2InBlackList.Value.ShouldBe(false);
        
        // User2 transfer should succeed after removal from blacklist
        user2TransferResult = await user2Stub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        user2TransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task MultiTokenContract_BatchAddToTransferBlackList_Test()
    {
        // Create and issue token using existing test method
        await MultiTokenContract_Approve_Test();
        
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        
        // Test BatchAddToTransferBlackList with Parliament when no controller is set (should succeed)
        var parliamentBatchAddProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.BatchAddToTransferBlackList), new BatchAddToTransferBlackListInput
            {
                Addresses = { User1Address }
            });
        await ApproveWithMinersAsync(parliamentBatchAddProposalId);
        await ParliamentContractStub.Release.SendAsync(parliamentBatchAddProposalId);
        
        // Verify User1Address is now in blacklist
        var isUser1InBlackList = await TokenContractStub.IsInTransferBlackList.CallAsync(User1Address);
        isUser1InBlackList.Value.ShouldBe(true);
        
        // Remove User1 from blacklist for later tests
        var removeUser1ProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.RemoveFromTransferBlackList), User1Address);
        await ApproveWithMinersAsync(removeUser1ProposalId);
        await ParliamentContractStub.Release.SendAsync(removeUser1ProposalId);
        
        // Setup Association contract and organization
        var associationStub = GetTester<AssociationContractImplContainer.AssociationContractImplStub>(AssociationContractAddress, DefaultKeyPair);
        var organizationCreated = await associationStub.CreateOrganization.SendAsync(new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = 1,
                MinimalVoteThreshold = 1,
                MaximalAbstentionThreshold = 0,
                MaximalRejectionThreshold = 0
            },
            ProposerWhiteList = new ProposerWhiteList
            {
                Proposers = { DefaultAddress }
            },
            OrganizationMemberList = new OrganizationMemberList
            {
                OrganizationMembers = { DefaultAddress }
            }
        });
        organizationCreated.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var organizationAddress = Address.Parser.ParseFrom(organizationCreated.TransactionResult.ReturnValue);
        
        // Set Association organization as TransferBlackListController via Parliament
        var changeControllerProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.ChangeTransferBlackListController), new AuthorityInfo
            {
                ContractAddress = AssociationContractAddress,
                OwnerAddress = organizationAddress
            });
        await ApproveWithMinersAsync(changeControllerProposalId);
        await ParliamentContractStub.Release.SendAsync(changeControllerProposalId);
        
        // Test BatchAddToTransferBlackList with empty input via Association organization should fail
        var emptyInputProposalId = await associationStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContractStub.BatchAddToTransferBlackList),
            ToAddress = TokenContractAddress,
            Params = new BatchAddToTransferBlackListInput().ToByteString(),
            OrganizationAddress = organizationAddress,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        });
        var emptyInputProposalHash = Hash.Parser.ParseFrom(emptyInputProposalId.TransactionResult.ReturnValue);
        await associationStub.Approve.SendAsync(emptyInputProposalHash);
        var emptyInputReleaseResult = await associationStub.Release.SendWithExceptionAsync(emptyInputProposalHash);
        emptyInputReleaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        emptyInputReleaseResult.TransactionResult.Error.ShouldContain("Invalid input");
        
        // Test BatchAddToTransferBlackList with unauthorized user should fail
        var batchAddUnauthorizedResult = await TokenContractStubUser.BatchAddToTransferBlackList.SendWithExceptionAsync(new BatchAddToTransferBlackListInput
        {
            Addresses = { User1Address, User2Address }
        });
        batchAddUnauthorizedResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        batchAddUnauthorizedResult.TransactionResult.Error.ShouldContain("No permission");
        
        // Transfer some tokens to user accounts first for testing
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User2Address
        });
        
        // Test BatchAddToTransferBlackList with Association organization controller
        var batchAddProposalId = await associationStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContractStub.BatchAddToTransferBlackList),
            ToAddress = TokenContractAddress,
            Params = new BatchAddToTransferBlackListInput
            {
                Addresses = { User1Address, User2Address, DefaultAddress }
            }.ToByteString(),
            OrganizationAddress = organizationAddress,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
        });
        var batchProposalHash = Hash.Parser.ParseFrom(batchAddProposalId.TransactionResult.ReturnValue);
        await associationStub.Approve.SendAsync(batchProposalHash);
        await associationStub.Release.SendAsync(batchProposalHash);
        
        // Verify all addresses are now in blacklist
        isUser1InBlackList = await TokenContractStub.IsInTransferBlackList.CallAsync(User1Address);
        isUser1InBlackList.Value.ShouldBe(true);
        var isUser2InBlackList = await TokenContractStub.IsInTransferBlackList.CallAsync(User2Address);
        isUser2InBlackList.Value.ShouldBe(true);
        var isDefaultInBlackList = await TokenContractStub.IsInTransferBlackList.CallAsync(DefaultAddress);
        isDefaultInBlackList.Value.ShouldBe(true);
        
        // Test that transfers from blacklisted addresses should fail
        var user1Stub = GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        var user1TransferResult = await user1Stub.Transfer.SendWithExceptionAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = DefaultAddress
        });
        user1TransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        user1TransferResult.TransactionResult.Error.ShouldContain("From address is in transfer blacklist");
        
        var user2Stub = GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User2KeyPair);
        var user2TransferResult = await user2Stub.Transfer.SendWithExceptionAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = DefaultAddress
        });
        user2TransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        user2TransferResult.TransactionResult.Error.ShouldContain("From address is in transfer blacklist");
        
        // Test Parliament can still remove from blacklist (RemoveFromTransferBlackList is not using new controller)
        var removeProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, nameof(TokenContractStub.RemoveFromTransferBlackList), User2Address);
        await ApproveWithMinersAsync(removeProposalId);
        await ParliamentContractStub.Release.SendAsync(removeProposalId);
        
        // Verify User2 is removed from blacklist
        isUser2InBlackList = await TokenContractStub.IsInTransferBlackList.CallAsync(User2Address);
        isUser2InBlackList.Value.ShouldBe(false);
        
        // User2 transfer should succeed after removal from blacklist
        user2TransferResult = await user2Stub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        user2TransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task MultiTokenContract_BatchRemoveFromTransferBlackList_Test()
    {
        // Create and issue token using existing test method
        await MultiTokenContract_Approve_Test();
        
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        
        // Transfer some tokens to user accounts for testing
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User2Address
        });
        
        // First, add multiple addresses to blacklist using BatchAddToTransferBlackList
        var batchAddProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.BatchAddToTransferBlackList), new BatchAddToTransferBlackListInput
            {
                Addresses = { User1Address, User2Address, DefaultAddress }
            });
        await ApproveWithMinersAsync(batchAddProposalId);
        await ParliamentContractStub.Release.SendAsync(batchAddProposalId);
        
        // Verify all addresses are in blacklist
        var user1InBlackListStatus = await TokenContractStub.IsInTransferBlackList.CallAsync(User1Address);
        user1InBlackListStatus.Value.ShouldBe(true);
        var user2InBlackListStatus = await TokenContractStub.IsInTransferBlackList.CallAsync(User2Address);
        user2InBlackListStatus.Value.ShouldBe(true);
        var defaultInBlackListStatus = await TokenContractStub.IsInTransferBlackList.CallAsync(DefaultAddress);
        defaultInBlackListStatus.Value.ShouldBe(true);
        
        // Test BatchRemoveFromTransferBlackList with empty input via Parliament should fail
        var emptyInputProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.BatchAddToTransferBlackList), new BatchRemoveFromTransferBlackListInput());
        await ApproveWithMinersAsync(emptyInputProposalId);
        var emptyInputReleaseResult = await ParliamentContractStub.Release.SendWithExceptionAsync(emptyInputProposalId);
        emptyInputReleaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        emptyInputReleaseResult.TransactionResult.Error.ShouldContain("Invalid input");
        
        // Test BatchRemoveFromTransferBlackList with unauthorized user should fail
        var unauthorizedRemoveResult = await TokenContractStubUser.BatchRemoveFromTransferBlackList.SendWithExceptionAsync(new BatchRemoveFromTransferBlackListInput
        {
            Addresses = { User1Address, User2Address }
        });
        unauthorizedRemoveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        unauthorizedRemoveResult.TransactionResult.Error.ShouldContain("Unauthorized behavior");
        
        // Test BatchRemoveFromTransferBlackList with Parliament authority (should succeed)
        var batchRemoveProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.BatchRemoveFromTransferBlackList), new BatchRemoveFromTransferBlackListInput
            {
                Addresses = { User1Address, User2Address }
            });
        await ApproveWithMinersAsync(batchRemoveProposalId);
        await ParliamentContractStub.Release.SendAsync(batchRemoveProposalId);
        
        // Verify User1 and User2 are removed from blacklist
        var user1BlackListStatusAfterRemove = await TokenContractStub.IsInTransferBlackList.CallAsync(User1Address);
        user1BlackListStatusAfterRemove.Value.ShouldBe(false);
        var user2BlackListStatusAfterRemove = await TokenContractStub.IsInTransferBlackList.CallAsync(User2Address);
        user2BlackListStatusAfterRemove.Value.ShouldBe(false);
        
        // Verify DefaultAddress is still in blacklist (not removed)
        var defaultBlackListStatusAfterRemove = await TokenContractStub.IsInTransferBlackList.CallAsync(DefaultAddress);
        defaultBlackListStatusAfterRemove.Value.ShouldBe(true);
        
        // Test that transfers from removed addresses should succeed
        var user1Stub = GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        var user1TransferResult = await user1Stub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount / 2,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = DefaultAddress
        });
        user1TransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        var user2Stub = GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User2KeyPair);
        var user2TransferResult = await user2Stub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount / 2,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        user2TransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        // Test that transfer from DefaultAddress (still in blacklist) should fail
        var defaultTransferResult = await TokenContractStub.Transfer.SendWithExceptionAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        defaultTransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        defaultTransferResult.TransactionResult.Error.ShouldContain("From address is in transfer blacklist");
        
        // Test BatchRemoveFromTransferBlackList with duplicate addresses (should handle gracefully)
        var duplicateRemoveProposalId = await CreateProposalAsync(TokenContractAddress, defaultParliament, 
            nameof(TokenContractStub.BatchRemoveFromTransferBlackList), new BatchRemoveFromTransferBlackListInput
            {
                Addresses = { DefaultAddress, DefaultAddress, DefaultAddress } // Duplicate addresses
            });
        await ApproveWithMinersAsync(duplicateRemoveProposalId);
        await ParliamentContractStub.Release.SendAsync(duplicateRemoveProposalId);
        
        // Verify DefaultAddress is removed from blacklist
        var defaultBlackListStatusFinal = await TokenContractStub.IsInTransferBlackList.CallAsync(DefaultAddress);
        defaultBlackListStatusFinal.Value.ShouldBe(false);
        
        // Test that transfer from DefaultAddress should now succeed
        defaultTransferResult = await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = Amount,
            Symbol = AliceCoinTokenInfo.Symbol,
            To = User1Address
        });
        defaultTransferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
}