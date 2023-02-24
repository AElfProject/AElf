using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
{
    private const string BasicFeeSymbol = "BASIC";
    private const string SizeFeeSymbol = "SIZE";
    private const string NativeToken = "ELF";

    [Fact]
    public async Task SetTokenDelegation_Test()
    {
        await Initialize();

        var delegations = new Dictionary<string, long>
        {
            [NativeToken] = 1000,
            [BasicFeeSymbol] = 500,
            [SizeFeeSymbol] = 100
        };
        await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput()
        {
            DelegatorAddress = User1Address,
            Delegations =
            {
                delegations
            }
        });

        var delegateAllowance = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput()
            {
                DelegateeAddress = DefaultAddress,
                DelegatorAddress = User1Address
            });
        delegateAllowance.Delegations[NativeToken].ShouldBe(1000);
        delegateAllowance.Delegations[BasicFeeSymbol].ShouldBe(500);
        delegateAllowance.Delegations[SizeFeeSymbol].ShouldBe(100);
        //
    }

    [Fact]
    public async Task SetTokenDelegation_removeDelegatee_Test()
    {
        await SetTokenDelegation_Test();
        var delegations = new Dictionary<string, long>
        {
            [NativeToken] = 0,
            [BasicFeeSymbol] = 0,
            [SizeFeeSymbol] = 0
        };
        await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput()
        {
            DelegatorAddress = User1Address,
            Delegations =
            {
                delegations
            }
        });

        var delegateAllowance = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput()
            {
                DelegateeAddress = DefaultAddress,
                DelegatorAddress = User1Address
            });
        delegateAllowance.Delegations.Count().ShouldBe(0);
    }

    [Fact]
    public async Task SetTokenDelegation_resetDelegatee_Test()
    {
        await SetTokenDelegation_Test();
        var delegations = new Dictionary<string, long>
        {
            [NativeToken] = 100,
            [BasicFeeSymbol] = 200
        };
        await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput()
        {
            DelegatorAddress = User1Address,
            Delegations =
            {
                delegations
            }
        });

        var delegateAllowance = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput()
            {
                DelegateeAddress = DefaultAddress,
                DelegatorAddress = User1Address
            });
        delegateAllowance.Delegations[NativeToken].ShouldBe(100);
        delegateAllowance.Delegations[BasicFeeSymbol].ShouldBe(200);
        delegateAllowance.Delegations[SizeFeeSymbol].ShouldBe(100);
    }

    [Fact]
    public async Task SetTokenDelegation_includeNegative_Test()
    {
        await SetTokenDelegation_Test();
        var delegations = new Dictionary<string, long>
        {
            [NativeToken] = -1,
            [BasicFeeSymbol] = 200
        };
        await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput()
        {
            DelegatorAddress = User1Address,
            Delegations =
            {
                delegations
            }
        });

        var delegateAllowance = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput()
            {
                DelegateeAddress = DefaultAddress,
                DelegatorAddress = User1Address
            });
        delegateAllowance.Delegations.Keys.ShouldNotContain(NativeToken);
        delegateAllowance.Delegations[BasicFeeSymbol].ShouldBe(200);
        delegateAllowance.Delegations[SizeFeeSymbol].ShouldBe(100);
    }

    [Fact]
    public async Task SetTokenDelegation_addNotExistToken_Test()
    {
        await SetTokenDelegation_Test();
        var TestToken = "NOTEXIST";
        var delegations = new Dictionary<string, long>
        {
            [TestToken] = 200,
        };

        var result = await TokenContractStub.SetTransactionFeeDelegations.SendWithExceptionAsync(
            new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = User1Address,
                Delegations =
                {
                    delegations
                }
            });
        result.TransactionResult.Error.ShouldContain("Token is not found");
    }


    private async Task Initialize()
    {
        await CreateBaseNativeTokenAsync();
        await CreateTokenAsync(DefaultAddress, BasicFeeSymbol);
        await CreateTokenAsync(DefaultAddress, SizeFeeSymbol);
    }

    private async Task CreateTokenAsync(Address creator, string tokenSymbol, bool isBurned = true)
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = tokenSymbol,
            TokenName = tokenSymbol + " name",
            TotalSupply = 1000_00000000,
            IsBurnable = isBurned,
            Issuer = creator,
        });
    }

    private async Task SetTest()
    {
        await Initialize();

        var delegations = new Dictionary<string, long>
        {
            [NativeToken] = 100,
            [BasicFeeSymbol] = 100,
            [SizeFeeSymbol] = 100
        };
        await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput
        {
            DelegatorAddress = User1Address,
            Delegations = { delegations }
        });

        var delegation = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput
            {
                DelegatorAddress = User1Address,
                DelegateeAddress = DefaultAddress
            });
        delegation.Delegations["ELF"].ShouldBe(100);
    }

    [Fact(DisplayName = "remove delegation test")]
    public async Task RemoveTransactionFeeDelegator_Test()
    {
        await SetTest();
        var executionResult = await TokenContractStub.RemoveTransactionFeeDelegator.SendAsync(
            new RemoveTransactionFeeDelegatorInput
            {
                DelegatorAddress = User1Address
            });
        var log = executionResult.TransactionResult.Logs
            .Where(e => e.Name.Contains(nameof(TransactionFeeDelegationCancelled))).Select(e => e.Indexed[0]);
        var delegationCancelled = TransactionFeeDelegationCancelled.Parser.ParseFrom(log.First());
        delegationCancelled.Delegator.ShouldBe(User1Address);
        var delegation = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput
            {
                DelegatorAddress = User1Address,
                DelegateeAddress = DefaultAddress
            });
        delegation.ShouldBe(new TransactionFeeDelegations());
    }

    [Fact(DisplayName = "remove delegation test")]
    public async Task RemoveTransactionFeeDelegator_Test_NotExist()
    {
        await SetTest();
        await TokenContractStub.RemoveTransactionFeeDelegator.SendAsync(
            new RemoveTransactionFeeDelegatorInput
            {
                DelegatorAddress = User2Address
            });
    }

    [Fact(DisplayName = "remove delegation test")]
    public async Task RemoveTransactionFeeDelegatee_Test()
    {
        await SetTest();
        var executionResult = await TokenContractStubUser.RemoveTransactionFeeDelegatee.SendAsync(
            new RemoveTransactionFeeDelegateeInput
            {
                DelegateeAddress = DefaultAddress
            });
        var log = executionResult.TransactionResult.Logs
            .Where(e => e.Name.Contains(nameof(TransactionFeeDelegationCancelled))).Select(e => e.Indexed[0]);
        var delegationCancelled = TransactionFeeDelegationCancelled.Parser.ParseFrom(log.First());
        delegationCancelled.Delegator.ShouldBe(User1Address);
        var delegation = await TokenContractStub.GetTransactionFeeDelegationsOfADelegatee.CallAsync(
            new GetTransactionFeeDelegationsOfADelegateeInput
            {
                DelegatorAddress = User1Address,
                DelegateeAddress = DefaultAddress
            });
        delegation.ShouldBe(new TransactionFeeDelegations());
    }

    [Fact(DisplayName = "remove delegation test")]
    public async Task RemoveTransactionFeeDelegatee_Test_NotExist()
    {
        await SetTest();
        await TokenContractStub.RemoveTransactionFeeDelegatee.SendAsync(
            new RemoveTransactionFeeDelegateeInput
            {
                DelegateeAddress = User2Address
            });
    }

    private async Task CreateBaseNativeTokenAsync()
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = NativeTokenInfo.Symbol,
            TokenName = NativeTokenInfo.TokenName,
            TotalSupply = NativeTokenInfo.TotalSupply,
            Decimals = NativeTokenInfo.Decimals,
            Issuer = NativeTokenInfo.Issuer,
            IsBurnable = NativeTokenInfo.IsBurnable,
        });
    }
    
    [Fact]
    public async Task GetTransactionFeeDelegatees_Test()
    {
        await Initialize();

        var delegations = new Dictionary<string, long>
        {
            [NativeToken] = 100,
            [BasicFeeSymbol] = 100,
            [SizeFeeSymbol] = 100
        };
        await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput
        {
            DelegatorAddress = User2Address,
            Delegations = { delegations }
        });
        await TokenContractStubUser.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput
        {
            DelegatorAddress = User2Address,
            Delegations = { delegations }
        });

        var output = await TokenContractStub.GetTransactionFeeDelegatees.CallAsync(new GetTransactionFeeDelegateesInput
        {
            DelegatorAddress = User2Address
        });
        output.DelegateeAddresses.Count.ShouldBe(2);
        output.DelegateeAddresses[0].ShouldBe(DefaultAddress);
        output.DelegateeAddresses[1].ShouldBe(User1Address);
    }
}