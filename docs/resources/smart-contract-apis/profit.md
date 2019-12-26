# Profit Contract

The Profit contract is an abstract layer for creating scheme to share bonus. Developers can build a system to distribute bonus by call this contract.

## **Scheme Create**:

The developer build a scheme
Q: ProfitReceivingDuePeriodCount < 0
```Protobuf
rpc CreateScheme (CreateSchemeInput) returns (aelf.Hash) {}

message CreateSchemeInput {
    sint64 profit_receiving_due_period_count = 1;
    bool is_release_all_balance_every_time_by_default = 2;
    sint32 delay_distribute_period_count = 3;
    aelf.Address manager = 4;
}

message Address
{
    bytes value = 1;
}

message Hash
{
    bytes value = 1;
}

message SchemeCreated {
    aelf.Address virtual_address = 1;
    aelf.Address manager = 2;
    sint64 profit_receiving_due_period_count = 3;
    bool is_release_all_balance_every_time_by_default = 4;
    aelf.Hash scheme_id = 5;
}
```

CreateScheme requires a **CreateSchemeInput** message as parameter:
- **profit receiving due period count** terms
- **is release all balance every time by default** distribute all
- **delay distribute period count** distribute bonus after terms
- **manager** the scheme manager

return
- **value** scheme id

context.fire
- **virtual address**  transfer from scheme id
- **manager** manager address
- **profit receiving due period count** how many terms
- **is release all balance every time by default** release all bonus
- **scheme id** scheme id


Sub scheme, subset of scheme, is in control of its parent scheme. Before adding it to a scheme, you should create it first. Just the manager can be authenticated.

```Protobuf
rpc AddSubScheme (AddSubSchemeInput) returns (google.protobuf.Empty) {}

message AddSubSchemeInput {
    aelf.Hash scheme_id = 1;
    aelf.Hash sub_scheme_id = 2;
    sint64 sub_scheme_shares = 3;
}
```

- **scheme id** target to be added scheme id
- **sub scheme id** sub scheme id
- **sub scheme shares** number of shares to sub scheme




Remove a sub scheme from the scheme
Q: useless property sub item creator?
```Protobuf
rpc RemoveSubScheme (RemoveSubSchemeInput) returns (google.protobuf.Empty) {}

message RemoveSubSchemeInput {
    aelf.Hash scheme_id = 1;
    aelf.Hash sub_scheme_id = 2;
    aelf.Address sub_item_creator = 3;
}
```

- **scheme id** scheme id
- **sub scheme id** sub scheme id
- **sub item creator** not used



Add a beneficiary to a scheme
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

AddBeneficiaryInput
- **scheme id** scheme id
- **beneficiary share** share information to beneficiary
- **end period** end time

BeneficiaryShare
- **beneficiary** beneficiary address
- **shares** shares to beneficiary



Remove a beneficiary(expiry）  from a scheme
Q: 没看懂移除过期那块的代码
```Protobuf
rpc RemoveBeneficiary (RemoveBeneficiaryInput) returns (google.protobuf.Empty) {}

message RemoveBeneficiaryInput {
    aelf.Address beneficiary = 1;
    aelf.Hash scheme_id = 2;
}
```

RemoveBeneficiaryInput
- **beneficiary** beneficiary address to be removed
- **scheme id** scheme id





Add a beneficiaries to a scheme
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

AddBeneficiariesInput
- **scheme id** scheme id
- **beneficiary shares** share information to beneficiaries
- **end period** end time

BeneficiaryShare
- **beneficiary** beneficiary address
- **shares** shares to beneficiary




Remove beneficiaries from a scheme

```Protobuf
 rpc RemoveBeneficiaries (RemoveBeneficiariesInput) returns (google.protobuf.Empty){}


message RemoveBeneficiariesInput {
    repeated aelf.Address beneficiaries = 1;
    aelf.Hash scheme_id = 2;
}
```

RemoveBeneficiariesInput
- **beneficiaries** beneficiaries' addresses to be removed
- **scheme id** scheme id
   



Contribute profit to a scheme
```Protobuf
rpc ContributeProfits (ContributeProfitsInput) returns (google.protobuf.Empty) {}

message ContributeProfitsInput {
    aelf.Hash scheme_id = 1;
    sint64 amount = 2;
    sint64 period = 3;
    string symbol = 4;
}
```

ContributeProfitsInput
- **scheme id** scheme id
- **amount** amount token contributed to the scheme
- **period** in which term the amount is added
- **symbol** token symbol



 distribute profits that should be distributed before current term to somebody/sub scheme
 Q:   in method ProfitAllPeriods
 period <= (profitDetail.EndPeriod == long.MaxValue
                    ? scheme.CurrentPeriod - 1
                    : Math.Min(scheme.CurrentPeriod - 1, profitDetail.EndPeriod)
```Protobuf
rpc ClaimProfits (ClaimProfitsInput) returns (google.protobuf.Empty) {}

message ClaimProfitsInput {
    aelf.Hash scheme_id = 1;
    string symbol = 2;
}
```

ContributeProfitsInput
- **scheme id** scheme id
- **symbol** token symbol



Distribute profits to scheme(address) including its sub scheme according to term and symbol,
should be called by the manager
```Protobuf

rpc DistributeProfits (DistributeProfitsInput) returns (google.protobuf.Empty) {}

message DistributeProfitsInput {
    aelf.Hash scheme_id = 1;
    sint64 period = 2;
    sint64 amount = 3;
    string symbol = 4;
}
```

DistributeProfitsInput
- **scheme id** scheme id
- **period** term， here should be the current term
- **amount** number
- **symbol** token symbol




Reset the manager of a scheme

```Protobuf
rpc ResetManager (ResetManagerInput) returns (google.protobuf.Empty) {}

message ResetManagerInput {
    aelf.Hash scheme_id = 1;
    aelf.Address new_manager = 2;
}
```

- **scheme id** scheme id
- **new manager** new manager











## view methods

For reference, you can find here the available view methods.


Get a manager's all scheme ids
```Protobuf
rpc GetManagingSchemeIds (GetManagingSchemeIdsInput) returns (CreatedSchemeIds) {}

message GetManagingSchemeIdsInput {
    aelf.Address manager = 1;
}

message CreatedSchemeIds {
    repeated aelf.Hash scheme_ids = 1;
}
```

GetManagingSchemeIdsInput
- **manager** manager address

CreatedSchemeIds
- **scheme ids**   scheme ids





Remove a candidate you voted
```Protobuf
rpc GetScheme (aelf.Hash) returns (Scheme) {}

message Hash
{
    bytes value = 1;
}

message Scheme {
    aelf.Address virtual_address = 1;
    sint64 total_shares = 2;
    map<string, sint64> undistributed_profits = 3;
    sint64 current_period = 4;
    repeated SchemeBeneficiaryShare sub_schemes = 5;
    sint64 profit_receiving_due_period_count = 7;
    bool is_release_all_balance_every_time_by_default = 8;
    aelf.Hash scheme_id = 9;
    sint32 delay_distribute_period_count = 10;
    map<sint64, sint64> cached_delay_total_shares = 11;
    aelf.Address manager = 12;
}

message SchemeBeneficiaryShare {
    aelf.Hash scheme_id = 1;
    sint64 shares = 2;
}
```

Hash
- **value** scheme id


Scheme
- **virtual address** computed by contract address and scheme id
- **total shares** total share
- **undistributed profits**undistributed profits  token symbol => amount
- **current period**current period
- **sub schemes** sub schemes
- **profit receiving due period count** how many terms
- **is release all balance every time by default** if input 0, release all one time
- **scheme id** scheme id
- **delay distribute period count** delay term to distribute profits
- **cached delay total shares** the scheme that will distribute profits in current term
- **manager**manager


SchemeBeneficiaryShare
- **scheme id** sub scheme's id
- **shares** sub scheme shares


Get scheme address by its id
Q:  when the is calculated by last id and itself? how to know its true id.
```Protobuf
rpc GetSchemeAddress (SchemePeriod) returns (aelf.Address) {}

message SchemePeriod {
    aelf.Hash scheme_id = 1;
    sint64 period = 2;
}

message Address
{
    bytes value = 1;
}
```

SchemePeriod
- **scheme_id** scheme id
- **period**  term number

Address

- **value**  scheme address



Get distributed profits Info.  total shares = -1  indicates failed to get the information
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

SchemePeriod
- **scheme_id** scheme id
- **period**  term number

DistributedProfitsInfo
- **total shares** total shares
- **profits amount** token symbol => reside amount
- **is released** is released





GetProfitDetails

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

GetProfitDetailsInput
- **scheme id** scheme id
- **beneficiary**  beneficiary

ProfitDetails
- **details** details

ProfitDetail
- **start period** start period
- **end period** end period
- **shares** shares
- **last profit period** last period the scheme distribute
- **is weight removed** is expired




Calculate profits(have not yet received) before current term(at most 10 term)
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

ClaimProfitsInput
- **scheme_id** scheme id
- **symbol**  token symbol


SInt64Value
- **value** amount of token
