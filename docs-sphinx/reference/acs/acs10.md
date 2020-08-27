# ACS10 -  Dividend Pool Standard

ACS10 is used to construct a dividend pool in the contract.

## Interface

To construct a dividend pool, you can implement the following interfaces optionally:

* Donate is used to donate dividend pool, parameters include the token symbol and the amount to be donated to the dividend pool;
* Release is used to release the dividend pool. The parameter is the number of sessions to release dividends. Be careful to set its calling permission.
* SetSymbolList is used to set the token symbols dividend pool supports. The parameter is of type SymbolList.
* GetSymbolList is used to get the token symbols dividend pool supports. The return type is SymbolList.
* GetUndistributdividends is used to obtain tokens' balance that have not been distributed. The return type is Dividends;
* GetDividends, whose return type is also Dividends, is used to obtain additional dividends from the height of a block.

SymbolList is a string list:

```proto
message SymbolList {
    repeated string value = 1;
}
```

The type of Dividends is a map from token symbol to amount:

```proto
message Dividends {
    map<string, int64> value = 1;
}
```

## Usage

ACS10 only unifies the standard interface of the dividend pool, which does not interact with the AElf chain.

## Implementaion

### With the Profit contract

A Profit Scheme can be created using the Profit contract's CreateScheme method:

```c#
State.ProfitContract.Value =
    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
var schemeToken = HashHelper.ComputeFrom(Context.Self);
State.ProfitContract.CreateScheme.Send(new CreateSchemeInput
{
    Manager = Context.Self,
    CanRemoveBeneficiaryDirectly = true,
    IsReleaseAllBalanceEveryTimeByDefault = true,
    Token = schemeToken
});
State.ProfitSchemeId.Value = Context.GenerateId(State.ProfitContract.Value, schemeToken);
```

The Context.GenerateId method is a common method used by the AElf to generate Id. We use the address of the Profit contract and the schemeToken provided to the Profit contract to calculate the Id of the scheme, and we set this id to State.ProfitSchemeId (SingletonState\<Hash\>).

After the establishment of the dividend scheme:

* ContributeProfits method of Profit can be used to implement the method Donate in ACS10.
* The Release in the ACS10 can be implemented using the method DistributeProfits in the Profit contract;
* Methods such as AddBeneficiary and RemoveBeneficiary can be used to manage the recipients and their weight.
* AddSubScheme, RemoveSubScheme and other methods can be used to manage the sub-dividend scheme and its weight;
* The SetSymbolList and GetSymbolList can be implemented by yourself. Just make sure the symbol list you set is used correctly in Donate and Release.
* GetUndistributedDividends returns the balance of the token whose symbol is includeded in symbol list.

### With TokenHolder Contract

When initializing the contract, you should create a token holder dividend scheme using the CreateScheme in the TokenHolder contract(Token Holder Profit Scheme）：

```c#
State.TokenHolderContract.Value =
    Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
State.TokenHolderContract.CreateScheme.Send(new CreateTokenHolderProfitSchemeInput
{
    Symbol = Context.Variables.NativeSymbol,
    MinimumLockMinutes = input.MinimumLockMinutes
});
return new Empty();
```

In a token holder dividend scheme, a scheme is bound to its creator, so SchemeId is not necessary to compute (in fact, the scheme is created via the Profit contract).

Considering the GetDividends returns the dividend information according to the input height, so each Donate need update dividend information for each height . A Donate can be implemented as:

```c#
public override Empty Donate(DonateInput input)
{
    State.TokenContract.TransferFrom.Send(new TransferFromInput
    {
        From = Context.Sender,
        Symbol = input.Symbol,
        Amount = input.Amount,
        To = Context.Self
    });
    State.TokenContract.Approve.Send(new ApproveInput
    {
        Symbol = input.Symbol,
        Amount = input.Amount,
        Spender = State.TokenHolderContract.Value
    });
    State.TokenHolderContract.ContributeProfits.Send(new ContributeProfitsInput
    {
        SchemeManager = Context.Self,
        Symbol = input.Symbol,
        Amount = input.Amount
    });
    Context.Fire(new DonationReceived
    {
        From = Context.Sender,
        Symbol = input.Symbol,
        Amount = input.Amount,
        PoolContract = Context.Self
    });
    var currentReceivedDividends = State.ReceivedDividends[Context.CurrentHeight];
    if (currentReceivedDividends != null && currentReceivedDividends.Value.ContainsKey(input.Symbol))
    {
        currentReceivedDividends.Value[input.Symbol] =
            currentReceivedDividends.Value[input.Symbol].Add(input.Amount);
    }
    else
    {
        currentReceivedDividends = new Dividends
        {
            Value =
            {
                {
                    input.Symbol, input.Amount
                }
            }
        };
    }
    State.ReceivedDividends[Context.CurrentHeight] = currentReceivedDividends;
    Context.LogDebug(() => string.Format("Contributed {0} {1}s to side chain dividends pool.", input.Amount, input.Symbol));
    return new Empty();
}
```

The method Release directly sends the TokenHolder's method DistributeProfits transaction:

```c#
public override Empty Release(ReleaseInput input)
{
    State.TokenHolderContract.DistributeProfits.Send(new DistributeProfitsInput
    {
        SchemeManager = Context.Self
    });
    return new Empty();
}
```

In the TokenHolder contract, the default implementation is to release what token is received, so SetSymbolList does not need to be implemented, and GetSymbolList returns the symbol list recorded in dividend scheme:

```c#
public override Empty SetSymbolList(SymbolList input)
{
    Assert(false, "Not support setting symbol list.");
    return new Empty();
}
public override SymbolList GetSymbolList(Empty input)
{
    return new SymbolList
    {
        Value =
        {
            GetDividendPoolScheme().ReceivedTokenSymbols
        }
    };
}
private Scheme GetDividendPoolScheme()
{
    if (State.DividendPoolSchemeId.Value == null)
    {
        var tokenHolderScheme = State.TokenHolderContract.GetScheme.Call(Context.Self);
        State.DividendPoolSchemeId.Value = tokenHolderScheme.SchemeId;
    }
    return Context.Call<Scheme>(
        Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName),
        nameof(ProfitContractContainer.ProfitContractReferenceState.GetScheme),
        State.DividendPoolSchemeId.Value);
}
```

The implementation of GetUndistributdividendeds is the same as described in the previous section, and it returns the balance:

```c#
public override Dividends GetUndistributedDividends(Empty input)
{
    var scheme = GetDividendPoolScheme();
    return new Dividends
    {
        Value =
        {
            scheme.ReceivedTokenSymbols.Select(s => State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = scheme.VirtualAddress,
                Symbol = s
            })).ToDictionary(b => b.Symbol, b => b.Balance)
        }
    };
}
```

In addition to the Profit and TokenHolder contracts, of course, you can also implement a dividend pool on your own contract.

## Test

The dividend pool, for example, is tested in two ways with the token Holder contract.

One way is for the dividend pool to send Donate, Release and a series of query operations;

The other way is to use an account to lock up, and then take out dividends.

Define the required Stubs:

```c#
const long amount = 10_00000000;
var keyPair = SampleECKeyPairs.KeyPairs[0];
var address = Address.FromPublicKey(keyPair.PublicKey);
var acs10DemoContractStub =
    GetTester<ACS10DemoContractContainer.ACS10DemoContractStub>(DAppContractAddress, keyPair);
var tokenContractStub =
    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
var tokenHolderContractStub =
    GetTester<TokenHolderContractContainer.TokenHolderContractStub>(TokenHolderContractAddress,
        keyPair);
```

Before proceeding, You should Approve the TokenHolder contract and the dividend pool contract. 

```c#
await tokenContractStub.Approve.SendAsync(new ApproveInput
{
    Spender = TokenHolderContractAddress,
    Symbol = "ELF",
    Amount = long.MaxValue
});
await tokenContractStub.Approve.SendAsync(new ApproveInput
{
    Spender = DAppContractAddress,
    Symbol = "ELF",
    Amount = long.MaxValue
});
```

Lock the position, at which point the account balance is reduced by 10 ELF:

```c#
await tokenHolderContractStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
{
    SchemeManager = DAppContractAddress,
    Amount = amount
});
```

Donate, at which point the account balance is reduced by another 10 ELF:

```c#
await acs10DemoContractStub.Donate.SendAsync(new DonateInput
{
    Symbol = "ELF",
    Amount = amount
});
```

At this point you can test the GetUndistributedDividends and GetDividends:

```c#
// Check undistributed dividends before releasing.
{
    var undistributedDividends =
        await acs10DemoContractStub.GetUndistributedDividends.CallAsync(new Empty());
    undistributedDividends.Value["ELF"].ShouldBe(amount);
}
var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
var currentBlockHeight = (await blockchainService.GetChainAsync()).BestChainHeight;
var dividends =
    await acs10DemoContractStub.GetDividends.CallAsync(new Int64Value {Value = currentBlockHeight});
dividends.Value["ELF"].ShouldBe(amount);
```

Release bonus, and test GetUndistributedDividends again:

```c#
await acs10DemoContractStub.Release.SendAsync(new ReleaseInput
{
    PeriodNumber = 1
});
// Check undistributed dividends after releasing.
{
    var undistributedDividends =
        await acs10DemoContractStub.GetUndistributedDividends.CallAsync(new Empty());
    undistributedDividends.Value["ELF"].ShouldBe(0);
}
```

Finally, let this account receive the dividend and then observe the change in its balance:

```c#
var balanceBeforeClaimForProfits = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput
{
    Owner = address,
    Symbol = "ELF"
});
await tokenHolderContractStub.ClaimProfits.SendAsync(new ClaimProfitsInput
{
    SchemeManager = DAppContractAddress,
    Beneficiary = address
});
var balanceAfterClaimForProfits = await tokenContractStub.GetBalance.CallAsync(new GetBalanceInput
{
    Owner = address,
    Symbol = "ELF"
});
balanceAfterClaimForProfits.Balance.ShouldBe(balanceBeforeClaimForProfits.Balance + amount);
```

## Example

The dividend pool of the main chain and the side chain is built by implementing ACS10.

The dividend pool provided by the Treasury contract implementing ACS10 is on the main chain.

The dividend pool provided by the ACS10 contract implementing ACS10 is on the side chain.
