using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.Collections;
using Shouldly;
using Volo.Abp;
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
        await CreateTokenAsync(DefaultAddress, BasicFeeSymbol);
        await CreateTokenAsync(DefaultAddress, SizeFeeSymbol);
    }

    private async Task CreateTokenAsync(Address creator, string tokenSymbol, bool isBurned = true)
    {
        await CreateMutiTokenAsync(TokenContractStub,new CreateInput
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

    [Fact]
    public async Task SetDelegateInfos_NewDelegate_Success_Test()
    {
        await Initialize();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 1000,
            [BasicFeeSymbol] = 500,
            [SizeFeeSymbol] = 100
        };
        var delegations2 = new Dictionary<string, long>
        {
            [NativeToken] = 100
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo2 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test2",
            Delegations =
            {
                delegations2
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo3 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test3",
            Delegations =
            {
                delegations2
            },
            IsUnlimitedDelegate = true
        };
        var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1,delegateInfo2,delegateInfo3 }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(1000);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(500);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test2"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(100);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test3"
                });
            delegateInfoOfADelegatee.Delegations.ShouldBeEmpty();
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeTrue();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var log = TransactionFeeDelegateInfoAdded.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoAdded))?.NonIndexed);
            log.Delegator.ShouldBe(User1Address);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.DelegateTransactionList.Value.Count.ShouldBe(3);
            log.DelegateTransactionList.Value[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            log.DelegateTransactionList.Value[0].MethodName.ShouldBe("test1");
            log.DelegateTransactionList.Value[1].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            log.DelegateTransactionList.Value[1].MethodName.ShouldBe("test2");
            log.DelegateTransactionList.Value[2].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            log.DelegateTransactionList.Value[2].MethodName.ShouldBe("test3");
        }

    }
    [Fact]
    public async Task SetDelegateInfos_NewOrUpdateDelegate_Success_Test()
    {
        await SetDelegateInfos_NewDelegate_Success_Test();
        var newDelegations = new Dictionary<string, long>
        {
            [NativeToken] = 10,
            [BasicFeeSymbol] = 20
        };
        var delegations2 = new Dictionary<string, long>
        {
            [NativeToken] = 100,
            [SizeFeeSymbol] = 30
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicContractZeroAddress,
            MethodName = "test1",
            Delegations =
            {
                newDelegations
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo2 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test2",
            Delegations =
            {
                delegations2
            },
            IsUnlimitedDelegate = false
        };
        var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1,delegateInfo2 }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicContractZeroAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(10);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(20);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test2"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(100);
            delegateInfoOfADelegatee.Delegations[SizeFeeSymbol].ShouldBe(30);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var log = TransactionFeeDelegateInfoAdded.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoAdded))?.NonIndexed);
            log.Delegator.ShouldBe(User1Address);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.DelegateTransactionList.Value.Count.ShouldBe(1);
            log.DelegateTransactionList.Value[0].ContractAddress.ShouldBe(BasicContractZeroAddress);
            log.DelegateTransactionList.Value[0].MethodName.ShouldBe("test1");
        }
        {
            var log = TransactionFeeDelegateInfoUpdated.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoUpdated))?.NonIndexed);
            log.Delegator.ShouldBe(User1Address);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.DelegateTransactionList.Value.Count.ShouldBe(1);
            log.DelegateTransactionList.Value[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            log.DelegateTransactionList.Value[0].MethodName.ShouldBe("test2");
        }

    }
    [Fact]
    public async Task SetDelegateInfos_UpdateDelegate_Success_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 300,
            [BasicFeeSymbol] = 200,
            [SizeFeeSymbol] = 100
        };
        var delegations3 = new Dictionary<string, long>
        {
            [NativeToken] = 100,
            [BasicFeeSymbol] = 200,
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo2 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test2",
            IsUnlimitedDelegate = true
        };
        var delegateInfo3 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test3",
            Delegations =
            {
                delegations3
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo4 = new DelegateInfo
        {
            ContractAddress = BasicContractZeroAddress,
            MethodName = "test1",
            IsUnlimitedDelegate = true
        };
        await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1,delegateInfo2,delegateInfo3,delegateInfo4 }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(300);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(200);
            delegateInfoOfADelegatee.Delegations[SizeFeeSymbol].ShouldBe(100);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test2"
                });
            delegateInfoOfADelegatee.Delegations.ShouldBeEmpty();
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeTrue();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test3"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(100);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(200);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicContractZeroAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.ShouldBeEmpty();
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeTrue();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
    }
    [Fact]
    public async Task SetDelegateInfos_UpdateDelegate_RemoveDelegation_Success_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 300,
            [BasicFeeSymbol] = 200,
            [SizeFeeSymbol] = -1
        };
        var delegations2 = new Dictionary<string, long>
        {
            [NativeToken] = 0,
            [SizeFeeSymbol] = 200,
        };
        var delegations3 = new Dictionary<string, long>
        {
            [NativeToken] = 5,
            [BasicFeeSymbol] = 0
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo2 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test2",
            Delegations =
            {
                delegations2
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo3 = new DelegateInfo
        {
            ContractAddress = BasicContractZeroAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations3
            },
            IsUnlimitedDelegate = false
        };
        var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1,delegateInfo2,delegateInfo3 }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(2);
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(300);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(200);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test2"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(1);
            delegateInfoOfADelegatee.Delegations[SizeFeeSymbol].ShouldBe(200);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicContractZeroAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(1);
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(5);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var log = TransactionFeeDelegateInfoUpdated.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoUpdated))?.NonIndexed);
            log.Delegator.ShouldBe(User1Address);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.DelegateTransactionList.Value.Count.ShouldBe(3);
            log.DelegateTransactionList.Value[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            log.DelegateTransactionList.Value[0].MethodName.ShouldBe("test1");
            log.DelegateTransactionList.Value[1].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            log.DelegateTransactionList.Value[1].MethodName.ShouldBe("test2");
            log.DelegateTransactionList.Value[2].ContractAddress.ShouldBe(BasicContractZeroAddress);
            log.DelegateTransactionList.Value[2].MethodName.ShouldBe("test1");
        }
    }
    [Fact]
    public async Task SetDelegateInfos_UpdateDelegate_RemoveDelegateInfo_Success_Test()
    {
        await SetDelegateInfos_NewDelegate_Success_Test();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = -1,
            [BasicFeeSymbol] = -1,
            [SizeFeeSymbol] = -1
        };
        var delegations2 = new Dictionary<string, long>
        {
            [NativeToken] = -1,
            [BasicFeeSymbol] = 200,
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo2 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test2",
            Delegations =
            {
                delegations2
            },
            IsUnlimitedDelegate = false
        };
        var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1,delegateInfo2 }
        });
        {
            var delegateeAddress = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    MethodName = "test1"
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeAddress = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    MethodName = "test2"
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(1);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test2"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(1);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(200);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var log = TransactionFeeDelegateInfoCancelled.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoCancelled))?.NonIndexed);
            log.Delegator.ShouldBe(User1Address);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.DelegateTransactionList.Value.Count.ShouldBe(1);
            log.DelegateTransactionList.Value[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            log.DelegateTransactionList.Value[0].MethodName.ShouldBe("test1");
        }
    }
    
    [Fact]
    public async Task SetDelegateInfos_AddOrUpdateOrRemoveDelegate_Success_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 300,
            [BasicFeeSymbol] = 200,
            [SizeFeeSymbol] = 0
        };
        var delegations4 = new Dictionary<string, long>
        {
            [NativeToken] = 60,
        };
        var delegations5 = new Dictionary<string, long>
        {
            [NativeToken] = 0,
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo2 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test2",
            IsUnlimitedDelegate = true
        };
        var delegateInfo4 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test4",
            Delegations =
            {
                delegations4
            },
            IsUnlimitedDelegate = false
        };
        var delegateInfo5 = new DelegateInfo
        {
            ContractAddress = BasicContractZeroAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations5
            },
            IsUnlimitedDelegate = false
        };
        var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1,delegateInfo2,delegateInfo4,delegateInfo5 }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(2);
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(300);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(200);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test2"
                });
            delegateInfoOfADelegatee.Delegations.ShouldBeEmpty();
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeTrue();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test4"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(60);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicContractZeroAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(1);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(20);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            {
                var log = TransactionFeeDelegateInfoAdded.Parser.ParseFrom(executionResult.TransactionResult.Logs
                    .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoAdded))?.NonIndexed);
                log.Delegator.ShouldBe(User1Address);
                log.Delegatee.ShouldBe(DefaultAddress);
                log.DelegateTransactionList.Value.Count.ShouldBe(1);
                log.DelegateTransactionList.Value[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
                log.DelegateTransactionList.Value[0].MethodName.ShouldBe("test4");
            }
            {
                var log = TransactionFeeDelegateInfoUpdated.Parser.ParseFrom(executionResult.TransactionResult.Logs
                    .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoUpdated))?.NonIndexed);
                log.Delegator.ShouldBe(User1Address);
                log.Delegatee.ShouldBe(DefaultAddress);
                log.DelegateTransactionList.Value.Count.ShouldBe(3);
                log.DelegateTransactionList.Value[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
                log.DelegateTransactionList.Value[0].MethodName.ShouldBe("test1");
                log.DelegateTransactionList.Value[1].ContractAddress.ShouldBe(BasicFunctionContractAddress);
                log.DelegateTransactionList.Value[1].MethodName.ShouldBe("test2");
                log.DelegateTransactionList.Value[2].ContractAddress.ShouldBe(BasicContractZeroAddress);
                log.DelegateTransactionList.Value[2].MethodName.ShouldBe("test1");
            }
        }
    }

    [Fact]
    public async Task SetDelegateInfos_MultiDelegateeAndSameTransaction_Success_Test()
    {
        await Initialize();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 1000,
            [BasicFeeSymbol] = 500,
            [SizeFeeSymbol] = 100
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        //Default -> User1
        await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1 }
        });
        //User2 -> User1
        await TokenContractStubDelegate.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1 }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(3);
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(1000);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = User2Address,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(3);
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(1000);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(2);
            delegateeList.DelegateeAddresses[0].ShouldBe(DefaultAddress);
            delegateeList.DelegateeAddresses[1].ShouldBe(User2Address);
        }
    }
    
    [Fact]
    public async Task SetDelegateInfos_MultiDelegatorAndSameTransaction_Success_Test()
    {
        await Initialize();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 1000,
            [BasicFeeSymbol] = 500,
            [SizeFeeSymbol] = 100
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        //Default -> User1
        await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1 }
        });
        //Default -> User2
        await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User2Address,
            DelegateInfoList = { delegateInfo1 }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(3);
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(1000);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User2Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations.Count.ShouldBe(3);
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(1000);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(1);
            delegateeList.DelegateeAddresses[0].ShouldBe(DefaultAddress);
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User2Address
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(1);
            delegateeList.DelegateeAddresses[0].ShouldBe(DefaultAddress);
        }
    }
    [Fact]
    public async Task SetDelegateInfos_InvalidInput_Failed_Test()
    {
        await Initialize();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 1000,
            [BasicFeeSymbol] = 500,
            [SizeFeeSymbol] = 100
        };
        var delegateInfo = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations = { delegations1 }
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            IsUnlimitedDelegate = false
        };
        var delegateInfo2 = new DelegateInfo
        {
            MethodName = "test1",
            Delegations = { delegations1 },
            IsUnlimitedDelegate = false
        };
        var delegateInfo3 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            Delegations = { delegations1 },
            IsUnlimitedDelegate = false
        };
        {
            await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = User1Address,
                DelegateInfoList = { delegateInfo }
            });
        }
        {
            var transactionFeeDelegation = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionFeeDelegation.IsUnlimitedDelegate.ShouldBeFalse();
            transactionFeeDelegation.Delegations.Count.ShouldBe(3);
            transactionFeeDelegation.Delegations[NativeToken].ShouldBe(1000);
        }
        {
            var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendWithExceptionAsync(new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = User1Address,
                DelegateInfoList = { delegateInfo1 }
            });
            executionResult.TransactionResult.Error.ShouldContain("Delegation cannot be null.");
        }
        {
            var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendWithExceptionAsync(new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = User1Address,
                DelegateInfoList = { delegateInfo2 }
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid contract address and method name.");
        }
        {
            var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendWithExceptionAsync(new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = User1Address,
                DelegateInfoList = { delegateInfo3 }
            });
            executionResult.TransactionResult.Error.ShouldContain("Invalid contract address and method name.");
        }
        {
            var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendWithExceptionAsync(new SetTransactionFeeDelegateInfosInput
            {
                DelegateInfoList = { delegateInfo }
            });
            executionResult.TransactionResult.Error.ShouldContain("Delegator address and delegate info cannot be null.");
        }
        {
            var executionResult = await TokenContractStub.SetTransactionFeeDelegateInfos.SendWithExceptionAsync(new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = User1Address,
            });
            executionResult.TransactionResult.Error.ShouldContain("Delegator address and delegate info cannot be null.");
        }
    }
    
    [Fact]
    public async Task RemoveTransactionFeeDelegateeInfos_Success_Test()
    {
        await SetDelegateInfos_NewDelegate_Success_Test();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 300,
            [BasicFeeSymbol] = 200
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        await TokenContractStubDelegate.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateInfoList = { delegateInfo1 }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken].ShouldBe(1000);
            delegateInfoOfADelegatee.Delegations[BasicFeeSymbol].ShouldBe(500);
            delegateInfoOfADelegatee.IsUnlimitedDelegate.ShouldBeFalse();
            delegateInfoOfADelegatee.BlockHeight.ShouldBeGreaterThan(0);
        }
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = BasicFunctionContractAddress,
                MethodName = "test1"
            },
            new DelegateTransaction
            {
                ContractAddress = BasicFunctionContractAddress,
                MethodName = "test3"
            }
        };
        await TokenContractStubUser.RemoveTransactionFeeDelegateeInfos.SendAsync(new RemoveTransactionFeeDelegateeInfosInput
        {
            DelegateeAddress = DefaultAddress,
            DelegateTransactionList = { delegationTransactionList }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test2"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken] = 100;
            delegateInfoOfADelegatee.IsUnlimitedDelegate = false;
        }
        {
            var delegateeAddress = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    MethodName = "test1"
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(1);
            delegateeAddress.DelegateeAddresses[0].ShouldBe(User2Address);
        }
    }

    [Fact]
    public async Task RemoveTransactionFeeDelegateeInfos_SingleTransaction_Success_Test()
    {
        await SetDelegateInfos_NewDelegate_Success_Test();
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = BasicFunctionContractAddress,
                MethodName = "test1"
            }
        };
        var executionResult = await TokenContractStubUser.RemoveTransactionFeeDelegateeInfos.SendAsync(new RemoveTransactionFeeDelegateeInfosInput
        {
            DelegateeAddress = DefaultAddress,
            DelegateTransactionList = { delegationTransactionList }
        });
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var log = TransactionFeeDelegateInfoCancelled.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoCancelled))?.NonIndexed);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.Delegator.ShouldBe(User1Address);
            delegationTransactionList.Count.ShouldBe(1);
            delegationTransactionList[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            delegationTransactionList[0].MethodName.ShouldBe("test1");
        }
    }
    [Fact]
    public async Task RemoveTransactionFeeDelegateeInfos_MultiTransaction_Success_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = BasicFunctionContractAddress,
                MethodName = "test1"
            },
            new DelegateTransaction
            {
                ContractAddress = BasicContractZeroAddress,
                MethodName = "test1"
            }
        };
        var executionResult = await TokenContractStubUser.RemoveTransactionFeeDelegateeInfos.SendAsync(new RemoveTransactionFeeDelegateeInfosInput
        {
            DelegateeAddress = DefaultAddress,
            DelegateTransactionList = { delegationTransactionList }
        });
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicContractZeroAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicContractZeroAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var log = TransactionFeeDelegateInfoCancelled.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoCancelled))?.NonIndexed);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.Delegator.ShouldBe(User1Address);
            delegationTransactionList.Count.ShouldBe(2);
            delegationTransactionList[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            delegationTransactionList[0].MethodName.ShouldBe("test1");
            delegationTransactionList[1].ContractAddress.ShouldBe(BasicContractZeroAddress);
            delegationTransactionList[1].MethodName.ShouldBe("test1");
        }
    }
    [Fact]
    public async Task RemoveTransactionFeeDelegateeInfos_NotExistTransaction_Success_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = TokenContractAddress,
                MethodName = "transfer"
            }
        };
        var executionResult = await TokenContractStubUser.RemoveTransactionFeeDelegateeInfos.SendAsync(new RemoveTransactionFeeDelegateeInfosInput
        {
            DelegateeAddress = DefaultAddress,
            DelegateTransactionList = { delegationTransactionList }
        });
        var executionResult1 = await TokenContractStubUser.RemoveTransactionFeeDelegateeInfos.SendAsync(new RemoveTransactionFeeDelegateeInfosInput
        {
            DelegateeAddress = User2Address,
            DelegateTransactionList = { delegationTransactionList }
        });
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = "transfer",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = "transfer",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = "transfer",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = User2Address
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = "transfer",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
    }

    [Fact]
    public async Task RemoveTransactionFeeDelegateeInfos_InvalidInput_Failed_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = BasicContractZeroAddress
            },
            new DelegateTransaction
            {
                MethodName = "jsh&&&"
            },
            new DelegateTransaction
            {
                
            }
        };
        var executionResult = await TokenContractStubUser.RemoveTransactionFeeDelegateeInfos.SendWithExceptionAsync(new RemoveTransactionFeeDelegateeInfosInput
        {
            DelegateeAddress = DefaultAddress,
            DelegateTransactionList = { delegationTransactionList }
        });
        executionResult.TransactionResult.Error.ShouldContain("Invalid contract address and method name.");
    }
     [Fact]
    public async Task RemoveTransactionFeeDelegatorInfos_Success_Test()
    {
        await SetDelegateInfos_NewDelegate_Success_Test();
        var delegations1 = new Dictionary<string, long>
        {
            [NativeToken] = 300,
            [BasicFeeSymbol] = 200
        };
        var delegateInfo1 = new DelegateInfo
        {
            ContractAddress = BasicFunctionContractAddress,
            MethodName = "test1",
            Delegations =
            {
                delegations1
            },
            IsUnlimitedDelegate = false
        };
        await TokenContractStub.SetTransactionFeeDelegateInfos.SendAsync(new SetTransactionFeeDelegateInfosInput
        {
            DelegatorAddress = User2Address,
            DelegateInfoList = { delegateInfo1 }
        });
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = BasicFunctionContractAddress,
                MethodName = "test1"
            },
            new DelegateTransaction
            {
                ContractAddress = BasicFunctionContractAddress,
                MethodName = "test3"
            }
        };
        await TokenContractStub.RemoveTransactionFeeDelegatorInfos.SendAsync(new RemoveTransactionFeeDelegatorInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateTransactionList = { delegationTransactionList }
        });
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test1"
                });
            delegateInfoOfADelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateInfoOfADelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress,
                    MethodName = "test2"
                });
            delegateInfoOfADelegatee.Delegations[NativeToken] = 100;
            delegateInfoOfADelegatee.IsUnlimitedDelegate = false;
        }
        {
            var delegateeAddress = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    DelegatorAddress = User2Address,
                    MethodName = "test1"
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(1);
            delegateeAddress.DelegateeAddresses[0].ShouldBe(DefaultAddress);
        }
    }
     [Fact]
    public async Task RemoveTransactionFeeDelegatorInfos_SingleTransaction_Success_Test()
    {
        await SetDelegateInfos_NewDelegate_Success_Test();
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = BasicFunctionContractAddress,
                MethodName = "test1"
            }
        };
        var executionResult = await TokenContractStub.RemoveTransactionFeeDelegatorInfos.SendAsync(new RemoveTransactionFeeDelegatorInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateTransactionList = { delegationTransactionList }
        });
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var log = TransactionFeeDelegateInfoCancelled.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoCancelled))?.NonIndexed);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.Delegator.ShouldBe(User1Address);
            delegationTransactionList.Count.ShouldBe(1);
            delegationTransactionList[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            delegationTransactionList[0].MethodName.ShouldBe("test1");
        }
    }
    [Fact]
    public async Task RemoveTransactionFeeDelegatorInfos_MultiTransaction_Success_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = BasicFunctionContractAddress,
                MethodName = "test1"
            },
            new DelegateTransaction
            {
                ContractAddress = BasicContractZeroAddress,
                MethodName = "test1"
            }
        };
        var executionResult = await TokenContractStub.RemoveTransactionFeeDelegatorInfos.SendAsync(new RemoveTransactionFeeDelegatorInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateTransactionList = { delegationTransactionList }
        });
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = BasicContractZeroAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicFunctionContractAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = BasicContractZeroAddress,
                    MethodName = "test1",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var log = TransactionFeeDelegateInfoCancelled.Parser.ParseFrom(executionResult.TransactionResult.Logs
                .FirstOrDefault(i => i.Name == nameof(TransactionFeeDelegateInfoCancelled))?.NonIndexed);
            log.Delegatee.ShouldBe(DefaultAddress);
            log.Delegator.ShouldBe(User1Address);
            delegationTransactionList.Count.ShouldBe(2);
            delegationTransactionList[0].ContractAddress.ShouldBe(BasicFunctionContractAddress);
            delegationTransactionList[0].MethodName.ShouldBe("test1");
            delegationTransactionList[1].ContractAddress.ShouldBe(BasicContractZeroAddress);
            delegationTransactionList[1].MethodName.ShouldBe("test1");
        }
    }
    [Fact]
    public async Task RemoveTransactionFeeDelegatorInfos_NotExistTransaction_Success_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = TokenContractAddress,
                MethodName = "transfer"
            }
        };
        var executionResult = await TokenContractStub.RemoveTransactionFeeDelegatorInfos.SendAsync(new RemoveTransactionFeeDelegatorInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateTransactionList = { delegationTransactionList }
        });
        var executionResult1 = await TokenContractStub.RemoveTransactionFeeDelegatorInfos.SendAsync(new RemoveTransactionFeeDelegatorInfosInput
        {
            DelegatorAddress = User2Address,
            DelegateTransactionList = { delegationTransactionList }
        });
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = "transfer",
                    DelegatorAddress = User1Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = "transfer",
                    DelegatorAddress = User1Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
        {
            var transactionDelegatee = await TokenContractStub.GetTransactionFeeDelegateInfo.CallAsync(
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = "transfer",
                    DelegatorAddress = User2Address,
                    DelegateeAddress = DefaultAddress
                });
            transactionDelegatee.ShouldBe(new TransactionFeeDelegations());
        }
        {
            var delegateeList = await TokenContractStub.GetTransactionFeeDelegateeList.CallAsync(
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = "transfer",
                    DelegatorAddress = User2Address,
                });
            delegateeList.DelegateeAddresses.Count.ShouldBe(0);
        }
    }

    [Fact]
    public async Task RemoveTransactionFeeDelegatorInfos_InvalidInput_Failed_Test()
    {
        await SetDelegateInfos_NewOrUpdateDelegate_Success_Test();
        var delegationTransactionList = new RepeatedField<DelegateTransaction>
        {
            new DelegateTransaction
            {
                ContractAddress = BasicContractZeroAddress
            },
            new DelegateTransaction
            {
                MethodName = "jsh&&&"
            },
            new DelegateTransaction
            {
                
            }
        };
        var executionResult = await TokenContractStub.RemoveTransactionFeeDelegatorInfos.SendWithExceptionAsync(new RemoveTransactionFeeDelegatorInfosInput
        {
            DelegatorAddress = User1Address,
            DelegateTransactionList = { delegationTransactionList }
        });
        executionResult.TransactionResult.Error.ShouldContain("Invalid contract address and method name.");
    }
}