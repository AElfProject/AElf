# Profit Contract

The Profit contract is an abstract layer for creating scheme to share bonus. Developers can build a system to distribute bonus by call this contract.

## **Scheme Creation**:

This method creates a new scheme based on the **CreateSchemeInput** message. 

```Protobuf
rpc CreateScheme (CreateSchemeInput) returns (aelf.Hash) {}

message CreateSchemeInput {
    sint64 profit_receiving_due_period_count = 1;
    bool is_release_all_balance_every_time_by_default = 2;
    sint32 delay_distribute_period_count = 3;
    aelf.Address manager = 4;
    bool can_remove_beneficiary_directly = 5;
    aelf.Hash token = 6;
}

message SchemeCreated {
    aelf.Address virtual_address = 1;
    aelf.Address manager = 2;
    sint64 profit_receiving_due_period_count = 3;
    bool is_release_all_balance_every_time_by_default = 4;
    aelf.Hash scheme_id = 5;
}
```

**CreateSchemeInput**:
- **manager**: the scheme manager's Address, defaults to the transaction sender.
- **profit receiving due period_count** optional, defaults to 10.
- **is release all balance every time by default** if true, all the schemes balance will be distributed during distribution if the input amount is 0.
- **delay distribute period count** distribute bonus after terms.
- **can remove beneficiary directly** indicates whether the beneficiary can be removed without considering its EndPeriod and IsWeightRemoved.
- **token** used to indicates scheme id.

**returns**:
- **value**: the newly created scheme id.

After a successful creation, a **SchemeCreated** event log can be found in the transaction result.

**SchemeCreated**:
- **virtual address**: transfer from scheme id.
- **manager**: manager address.
- **scheme id**: scheme id.

## **Add sub-scheme**

Two previously created schemes can be put in a scheme/sub-scheme relation. This will effectively add the specified sub-scheme as a **beneficiary** of the parent scheme.

```Protobuf
rpc AddSubScheme (AddSubSchemeInput) returns (google.protobuf.Empty) {}

message AddSubSchemeInput {
    aelf.Hash scheme_id = 1;
    aelf.Hash sub_scheme_id = 2;
    sint64 sub_scheme_shares = 3;
}
```

- **scheme id**: the parent scheme ID.
- **sub scheme id**: the child scheme ID.
- **sub scheme shares**: number of shares of the sub-scheme.

## **Remove sub-scheme**:

Removes a sub-scheme from a scheme. Note that only the manager of the parent scheme can remove a sub-scheme from it.

```Protobuf
rpc RemoveSubScheme (RemoveSubSchemeInput) returns (google.protobuf.Empty) {}

message RemoveSubSchemeInput {
    aelf.Hash scheme_id = 1;
    aelf.Hash sub_scheme_id = 2;
}
```

**RemoveSubSchemeInput**:
- **scheme id**: scheme id
- **sub scheme id**: sub-scheme id

## **Add beneficiary**

Adds a beneficiary to a scheme. This beneficiary is either a scheme or another entity that can be represented by an AElf address.

```Protobuf
rpc AddBeneficiary (AddBeneficiaryInput) returns (google.protobuf.Empty) {}

message AddBeneficiaryInput {
    aelf.Hash scheme_id = 1;
    BeneficiaryShare beneficiary_share = 2;
    sint64 end_period = 3;
}

message BeneficiaryShare {
    aelf.Address beneficiary = 1;
    sint64 shares = 2;
}
```

**AddBeneficiaryInput**:
- **scheme id**: scheme id.
- **beneficiary share**: share information to beneficiary.
- **end period**: end time.

**BeneficiaryShare**:
- **beneficiary**: beneficiary address
- **shares**: shares attributed to this beneficiary.

## **Remove beneficiary**

Removes a beneficiary from a scheme.

```Protobuf
rpc RemoveBeneficiary (RemoveBeneficiaryInput) returns (google.protobuf.Empty) {}

message RemoveBeneficiaryInput {
    aelf.Address beneficiary = 1;
    aelf.Hash scheme_id = 2;
}
```

**RemoveBeneficiaryInput**:
- **beneficiary** beneficiary address to be removed
- **scheme id** scheme id


## **Add beneficiaries**

Adds multiple beneficiaries to a scheme until the given end period.

```Protobuf
rpc AddBeneficiaries (AddBeneficiariesInput) returns (google.protobuf.Empty) {}

message AddBeneficiariesInput {
    aelf.Hash scheme_id = 1;
    repeated BeneficiaryShare beneficiary_shares = 2;
    sint64 end_period = 4;
}

message BeneficiaryShare {
    aelf.Address beneficiary = 1;
    sint64 shares = 2;
}
```

**AddBeneficiariesInput**:
- **scheme id**: scheme id.
- **beneficiary shares**: share information to beneficiaries.
- **end period**: end time.

**BeneficiaryShare**
- **beneficiary**: beneficiary address.
- **shares**: shares to beneficiary.

## **Remove beneficiaries**

Remove beneficiaries from a scheme.

```Protobuf
 rpc RemoveBeneficiaries (RemoveBeneficiariesInput) returns (google.protobuf.Empty){}

message RemoveBeneficiariesInput {
    repeated aelf.Address beneficiaries = 1;
    aelf.Hash scheme_id = 2;
}
```

RemoveBeneficiariesInput
- **beneficiaries** beneficiaries' addresses to be removed.
- **scheme id** scheme id.
   

## **Profit contribution**

Contribute profit to a scheme.

```Protobuf
rpc ContributeProfits (ContributeProfitsInput) returns (google.protobuf.Empty) {}

message ContributeProfitsInput {
    aelf.Hash scheme_id = 1;
    sint64 amount = 2;
    sint64 period = 3;
    string symbol = 4;
}
```

**ContributeProfitsInput**:
- **scheme id**: scheme id.
- **amount**: amount token contributed to the scheme.
- **period**: in which term the amount is added.
- **symbol**: token symbol.

## **Claim profits**

Used to claim the profits of a given symbol. The beneficiary is identified as the sender of the transaction.

```Protobuf
rpc ClaimProfits (ClaimProfitsInput) returns (google.protobuf.Empty) {}

message ClaimProfitsInput {
    aelf.Hash scheme_id = 1;
    string symbol = 2;
    aelf.Address beneficiary = 3;
}
```

**ContributeProfitsInput**:
- **scheme id**: scheme id.
- **symbol**: token symbol.
- **beneficiary**: optional, claiming profits for another address, transaction fees apply to the caller.

## **Distribute profits**

Distribute profits to scheme (address) including its sub scheme according to term and symbol,
should be called by the manager.

```Protobuf

rpc DistributeProfits (DistributeProfitsInput) returns (google.protobuf.Empty) {}

message DistributeProfitsInput {
    aelf.Hash scheme_id = 1;
    sint64 period = 2;
    sint64 amount = 3;
    string symbol = 4;
}
```

**DistributeProfitsInput**:
- **scheme id**: scheme id.
- **period**: term， here should be the current term.
- **amount**: number.
- **symbol**: token symbol.

## **Reset manager**

Reset the manager of a scheme.

```Protobuf
rpc ResetManager (ResetManagerInput) returns (google.protobuf.Empty) {}

message ResetManagerInput {
    aelf.Hash scheme_id = 1;
    aelf.Address new_manager = 2;
}
```

**ResetManagerInput**:
- **scheme id**: scheme id.
- **new manager**: new manager's address.

## view methods

For reference, you can find here the available view methods.

### GetManagingSchemeIds

Get all schemes created by the specified manager.

```Protobuf
rpc GetManagingSchemeIds (GetManagingSchemeIdsInput) returns (CreatedSchemeIds) {}

message GetManagingSchemeIdsInput {
    aelf.Address manager = 1;
}

message CreatedSchemeIds {
    repeated aelf.Hash scheme_ids = 1;
}
```

**GetManagingSchemeIdsInput**:
- **manager**: manager's address.

**returns**:
- **scheme ids**: list of scheme ids.

### GetScheme

Returns the scheme with the given hash (scheme ID).

```Protobuf
rpc GetScheme (aelf.Hash) returns (Scheme) {}
```

**Hash**:
- **value**: scheme id.

**SchemeBeneficiaryShare**:
- **scheme id**: sub scheme's id.
- **shares**: sub scheme shares.

### GetSchemeAddress

Returns the schemes virtual address if the input period is 0 or will give the distributed profit address for the given period.

```Protobuf
rpc GetSchemeAddress (SchemePeriod) returns (aelf.Address) {}

message SchemePeriod {
    aelf.Hash scheme_id = 1;
    sint64 period = 2;
}
```

**SchemePeriod**:
- **scheme id**: scheme id.
- **period**: period number.

**returns**:
- **value**: scheme's virtual address.

### GetDistributedProfitsInfo

Get distributed profits Info for a given period.

```Protobuf
rpc GetDistributedProfitsInfo (SchemePeriod) returns (DistributedProfitsInfo) {}

message SchemePeriod {
    aelf.Hash scheme_id = 1;
    sint64 period = 2;
}

message DistributedProfitsInfo {
    sint64 total_shares = 1;
    map<string, sint64> profits_amount = 2;
    bool is_released = 3;
}
```

**SchemePeriod**:
- **scheme_id**: scheme id.
- **period**:  term number.

**returns**:
- **total shares**: total shares, -1 indicates failed to get the information.
- **profits amount**: token symbol => reside amount.
- **is released**: is released.

### GetProfitDetails

Gets a beneficiaries profit details for a given scheme.

```Protobuf
rpc GetProfitDetails (GetProfitDetailsInput) returns (ProfitDetails) {}

message GetProfitDetailsInput {
    aelf.Hash scheme_id = 1;
    aelf.Address beneficiary = 2;
}

message ProfitDetails {
    repeated ProfitDetail details = 1;
}

message ProfitDetail {
    sint64 start_period = 1;
    sint64 end_period = 2;
    sint64 shares = 3;
    sint64 last_profit_period = 4;
    bool is_weight_removed = 5;
}
```

**GetProfitDetailsInput**:
- **scheme id** scheme id.
- **beneficiary**  beneficiary.

**returns**:
- **details** profit details.

**ProfitDetail**:
- **start period**: start period.
- **end period**: end period.
- **shares**: shares indicating the weight used to calculate the profit in the future.
- **last profit period**: last period the scheme distribute.
- **is weight removed**: is it expired.

### GetProfitAmount

Calculate profits (have not yet received) before current term(at most 10 term).

```Protobuf
rpc GetProfitAmount (ClaimProfitsInput) returns (aelf.SInt64Value) {}

message ClaimProfitsInput {
    aelf.Hash scheme_id = 1;
    string symbol = 2;
}

message SInt64Value
{
    sint64 value = 1;
}
```

**ClaimProfitsInput**:
- **scheme_id**: scheme id.
- **symbol**: token symbol.

**returns**:
- **value**: amount of tokens.
