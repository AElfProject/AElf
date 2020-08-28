# ACS5 - Contract Threshold Standard

If you want to raise the threshold for using contract, consider implementing ACS5.

## Interface

To limit to call a method in a contract, you only need to implement an interface:

* GetMethodCallingThreshold, the parameter is string, and the return value is the MethodCallingThreshold defined in the acs5.proto file.

If you want to modify the threshold after the contract is deployed, another interface can be implemented:

* SetMethodCallingThreshold, the parameter is SetMethodCallingThresholdInput.

The definition of MethodCallingThreshold type is:

```proto
message MethodCallingThreshold {
    map<string, int64> symbol_to_amount = 1;// The order matters.
    ThresholdCheckType threshold_check_type = 2;
}
enum ThresholdCheckType {
    BALANCE = 0;
    ALLOWANCE = 1;
}
```

The significance of the enumeration ThresholdCheckType is that there are two types of thresholds for contract method calls:

1. It can be called when the balance of a certain token in the account is sufficient, which corresponds to ThresholdCheckType.Balance;
2. Not only does the balance of a token in the account be required to be sufficient, but the account also needs sufficient authorization for the target contract, which corresponds to The ThresholdCheckType.Allowance.
3. SetMethodCallingThresholdInput definition：

```proto
message SetMethodCallingThresholdInput {
    string method = 1;
    map<string, int64> symbol_to_amount = 2;// The order matters.
    ThresholdCheckType threshold_check_type = 3;
}
```

## Usage

Similar to ACS1, which uses an automatically generated pre-plugin transaction called ChargeTransactionFees to charge a transaction fee, ACS5 automatically generates a pre-plugin transaction called CheckThreshold to test whether the account that sent the transaction can invoke the corresponding method.

The implementation of CheckThreshold:

```c#
public override Empty CheckThreshold(CheckThresholdInput input)
{
    var meetThreshold = false;
    var meetBalanceSymbolList = new List<string>();
    foreach (var symbolToThreshold in input.SymbolToThreshold)
    {
        if (GetBalance(input.Sender, symbolToThreshold.Key) < symbolToThreshold.Value)
            continue;
        meetBalanceSymbolList.Add(symbolToThreshold.Key);
    }
    if (meetBalanceSymbolList.Count > 0)
    {
        if (input.IsCheckAllowance)
        {
            foreach (var symbol in meetBalanceSymbolList)
            {
                if (State.Allowances[input.Sender][Context.Sender][symbol] <
                    input.SymbolToThreshold[symbol]) continue;
                meetThreshold = true;
                break;
            }
        }
        else
        {
            meetThreshold = true;
        }
    }
    if (input.SymbolToThreshold.Count == 0)
    {
        meetThreshold = true;
    }
    Assert(meetThreshold, "Cannot meet the calling threshold.");
    return new Empty();
}
```

In other words, if the token balance of the sender of the transaction or the amount authorized for the target contract does not reach the set limit, the pre-plugin transaction will throw an exception, thereby it prevents the original transaction from executing.

## Implementation

As the GetMethodFee of ACS1, you can implement only one GetMethodCallingThreshold method.

It can also be achieved by using MappedState<string, MethodCallingThreshold> in the State file:

```c#
public MappedState<string, MethodCallingThreshold> MethodCallingThresholds { get; set; }
```

But at the same time, do not forget to configure the call permission of SetMethodCallingThreshold, which requires the definition of an Admin in the State (of course, you can also use ACS3):

```c#
public SingletonState<Address> Admin { get; set; }
```

The easiest implementation：

```c#
public override Empty SetMethodCallingThreshold(SetMethodCallingThresholdInput input)
{
    Assert(State.Admin.Value == Context.Sender, "No permission.");
    State.MethodCallingThresholds[input.Method] = new MethodCallingThreshold
    {
        SymbolToAmount = {input.SymbolToAmount}
    };
    return new Empty();
}
public override MethodCallingThreshold GetMethodCallingThreshold(StringValue input)
{
    return State.MethodCallingThresholds[input.Value];
}
public override Empty Foo(Empty input)
{
    return new Empty();
}
```

## Test

You can test the Foo method defined above.

Make a Stub:

```c#
var keyPair = SampleECKeyPairs.KeyPairs[0];
var acs5DemoContractStub =
    GetTester<ACS5DemoContractContainer.ACS5DemoContractStub>(DAppContractAddress, keyPair);
```

Before setting the threshold, check the current threshold, which should be 0:

```c#
var methodResult = await acs5DemoContractStub.GetMethodCallingThreshold.CallAsync(
    new StringValue
    {
        Value = nameof(acs5DemoContractStub.Foo)
    });
methodResult.SymbolToAmount.Count.ShouldBe(0);
```

The ELF balance of the caller of Foo should be greater than 1 ELF:

```c#
await acs5DemoContractStub.SetMethodCallingThreshold.SendAsync(
    new SetMethodCallingThresholdInput
    {
        Method = nameof(acs5DemoContractStub.Foo),
        SymbolToAmount =
        {
            {"ELF", 1_0000_0000}
        },
        ThresholdCheckType = ThresholdCheckType.Balance
    });
```

Check the threshold again:

```c#
methodResult = await acs5DemoContractStub.GetMethodCallingThreshold.CallAsync(
    new StringValue
    {
        Value = nameof(acs5DemoContractStub.Foo)
    });
methodResult.SymbolToAmount.Count.ShouldBe(1);
methodResult.ThresholdCheckType.ShouldBe(ThresholdCheckType.Balance);
```

Send the Foo transaction via an account who has sufficient balance can succeed:

```c#
// Call with enough balance.
{
    var executionResult = await acs5DemoContractStub.Foo.SendAsync(new Empty());
    executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
}
```

Send the Foo transaction via another account without ELF fails:

```c#
// Call without enough balance.
{
    var poorStub =
        GetTester<ACS5DemoContractContainer.ACS5DemoContractStub>(DAppContractAddress,
            SampleECKeyPairs.KeyPairs[1]);
    var executionResult = await poorStub.Foo.SendWithExceptionAsync(new Empty());
    executionResult.TransactionResult.Error.ShouldContain("Cannot meet the calling threshold.");
}
```
