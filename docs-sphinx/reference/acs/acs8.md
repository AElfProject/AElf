# ACS8 - Transaction Resource Token Fee Standard

ACS8 has some similarities to ACS1, both of them are charge transaction fee standard.

The difference is that ACS1 charges the user a transaction fee, ACS8 charges the called contract, and the transaction fee charged by ACS8 is the specified four tokens: WRITE, READ, NET, TRAFFIC.

In another word, if a contract declares that it inherits from ACS8, each transaction in this contract will charge four kinds of resource token.

## Interface

Only one method is defined in the acs8.proto file:

* BuyResourceToken, the parameter is the BuyResourceTokenInput defined in the Proto file.

```proto
message BuyResourceTokenInput {
    string symbol = 1;
    int64 amount = 2;
    int64 pay_limit = 3; // No buy if paying more than this, 0 if no limit
}
```

This method can be used to purchase one of the four resource coins, which consumes the ELF balance in the contract account (you can recharge it yourself, or you can collect the user's ELF tokens as a profit to be self-sufficient).

Of course, it is possible for developers to purchase resource token and then directly transfer to the address of this contract via the Transfer method of the Token contract. Therefore, this interface does not have to be implemented.

## Usage

The contract inherited from ACS1 uses a pre-plugin transaction called ChargeTransactionFees for charging transaction fee.

Because the specific charge amount is determined by the actual consumption of the transaction, the post-plugin generates ChargeResourceToken tansaction to charge resource token.

The implementation of ChargeResourceToken is also similar to it of ChargeTransactionFees:

```c#
public override Empty ChargeResourceToken(ChargeResourceTokenInput input)
{
    Context.LogDebug(() => string.Format("Start executing ChargeResourceToken.{0}", input));
    if (input.Equals(new ChargeResourceTokenInput()))
    {
        return new Empty();
    }
    var bill = new TransactionFeeBill();
    foreach (var pair in input.CostDic)
    {
        Context.LogDebug(() => string.Format("Charging {0} {1} tokens.", pair.Value, pair.Key));
        var existingBalance = GetBalance(Context.Sender, pair.Key);
        Assert(existingBalance >= pair.Value,
            string.Format("Insufficient resource of {0}. Need balance: {1}; Current balance: {2}.", pair.Key, pair.Value, existingBalance));
        bill.FeesMap.Add(pair.Key, pair.Value);
    }
    foreach (var pair in bill.FeesMap)
    {
        Context.Fire(new ResourceTokenCharged
        {
            Symbol = pair.Key,
            Amount = pair.Value,
            ContractAddress = Context.Sender
        });
        if (pair.Value == 0)
        {
            Context.LogDebug(() => string.Format("Maybe incorrect charged resource fee of {0}: it's 0.", pair.Key));
        }
    }
    return new Empty();
}
```

The amount of each resource token should be calculated by AElf.Kernel.FeeCalculation. In detail, A data structure named CalculateFeeCoefficients is defined in token_contract.proto, whose function is to save all coefficients of a polynomial, and every three coefficients are a group, such as a, b, c, which means (b / c) * x ^ a. Each resource token has a polynomial that calculates it. Then according to the polynomial and the actual consumption of the resource, calculate the cost of the resource token. Finally, the cost is used as the parameter of ChargeResourceToken to generate this post-plugin transaction.

In addition, the method of the contract that has been owed cannot be executed before the contract top up resource token. As a result, a pre-plugin transaction is added, similar to the ACS5 pre-plugin transaction, which checks the contract's resource token balance, and the transaction's method name is CheckResourceToken :

```c#
public override Empty CheckResourceToken(Empty input)
{
    foreach (var symbol in Context.Variables.GetStringArray(TokenContractConstants.PayTxFeeSymbolListName))
    {
        var balance = GetBalance(Context.Sender, symbol);
        var owningBalance = State.OwningResourceToken[Context.Sender][symbol];
        Assert(balance > owningBalance,
            string.Format("Contract balance of {0} token is not enough. Owning {1}.", symbol, owningBalance));
    }
    return new Empty();
}
```
