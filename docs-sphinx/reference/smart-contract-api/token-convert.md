# TokenConvert Contract

Using this contract can build a connection between the base token(the default is native token) and other tokens created on the chain. After building the connection, users can trade tokens with the Bancor model. You can find the detail information about Bancor in AElf Economic System White Paper.

## **InitializeInput**

This method is used to initialize this contract (add base token, connections, etc.).

```Protobuf
rpc Initialize (InitializeInput) returns (google.protobuf.Empty) {
}

message InitializeInput {
    string base_token_symbol = 1;
    string fee_rate = 2;
    repeated Connector connectors = 3;
}

message Connector {
    string symbol = 1;
    int64 virtual_balance = 2;
    string weight = 3;
    bool is_virtual_balance_enabled = 4;
    bool is_purchase_enabled = 5;
    string related_symbol = 6;
    bool is_deposit_account = 7;
}
```

**InitializeInput**:

- **base token symbol**: base token, default is the native token.
- **fee rate**: buy/sell token need pay the fee( = cost * feeRate).
- **connectors**: the default added connectors.

**Connector**:

We use Bancor model to build the connection between base token and other tokens. Each pair connectors include the coefficients used by calculating the token price based on the base token, and it consists of the base token connector and the new token connector.

- **symbol**: the connector symbol.
- **related symbol**: indicates its related connector, the pair connector includes a new created token connector and the base token connector.
- **virtual balance**: used in bancor model.
- **weight**: the weight is linked to the related connector's weight.
- **is virtual balance enabled**: true indicates that the virtual balance is used in price calculation.
- **is purchase enabled**: after build a pair connector, you can not buy/sell the token immediately, the default is false.
- **is deposit account**: indicates if the connector is base token connector.

## **AddPairConnector**

With Bancor model, each new token need a pair connectors to calculate its price. Only the connector contoller(the default is parliament) is allowed to call this API.

```Protobuf
rpc AddPairConnector(PairConnectorParam) returns (google.protobuf.Empty){
}

message PairConnectorParam {
    string resource_connector_symbol = 1;
    string resource_weight = 2;
    int64 native_virtual_balance = 3;
    string native_weight = 4;
}
```

**PairConnectorParam**:

- **resource connector symbol**: the new token connector's symbol.
- **resource weight**: the new token connector's weight.
- **native virtual balance**: the related base token connector's virtual balance. 
- **native weight**: base token's weight.

## **EnableConnector**

To make the connection work, the connector contoller need send this transaction.

```Protobuf
rpc EnableConnector (ToBeConnectedTokenInfo) returns (google.protobuf.Empty) {
}

message ToBeConnectedTokenInfo{
    string token_symbol = 1;
    int64 amount_to_token_convert = 2;
}
```

**ToBeConnectedTokenInfo**:

- **token symbol**: the token symbol.
- **amount to token convert**: to make the token trade, maybe you need deposit some base token.

## **UpdateConnector**

Before calling the EnableConnector, the connector contoller update the pair connctors' information.

```Protobuf
rpc UpdateConnector(Connector) returns (google.protobuf.Empty){
}
```

note: *for Connector see Initialize*

## **Buy**

After building the connection and enabling it, you can buy the new token with the base token.

```Protobuf
rpc Buy (BuyInput) returns (google.protobuf.Empty) {
}

message BuyInput {
    string symbol = 1;
    int64 amount = 2;
    int64 pay_limit = 3;
}
```

**BuyInput**:

- **symbol**: the token symbol.
- **amount**: the amount you want to buy.
- **pay limit**: no buy if paying more than this, 0 if no limit.

## **Sell**

After building the connection and enabling it, you can sell the new token.

```Protobuf
rpc Sell (SellInput) returns (google.protobuf.Empty) {
}

message SellInput {
    string symbol = 1;
    int64 amount = 2;
    int64 receive_limit = 3;
}
```

**SellInput**:

- **symbol**: the token symbol.
- **amount**: the amount you want to sell.
- **receive limit**: no sell if receiving less than this, 0 if no limit.

## **ChangeConnectorController**

The controller can be transfer to others.

```Protobuf
rpc ChangeConnectorController (acs1.AuthorityInfo) returns (google.protobuf.Empty) {
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

**AuthorityInfo**:

- **contract address**: new controller's contract address.
- **owner address**: new controller's address.

## ACS1 Implementation

For reference, you can find here the methods implementing acs1.

### SetMethodFee

It sets method fee.

```Protobuf
rpc SetMethodFee (MethodFees) returns (google.protobuf.Empty){
}

message MethodFees {
    string method_name = 1;
    repeated MethodFee fees = 2;
}

message MethodFee {
    string symbol = 1;
    int64 basic_fee = 2;
}
```

**MethodFees**:

- **method name** the method name in this contract.
- **fees** fee list.

**MethodFee**:

- **symbol** token symbol.
- **basic fee** fee.

### ChangeMethodFeeController

Change the method fee controller, the default is Parliament.

```Protobuf
rpc ChangeMethodFeeController (AuthorityInfo) returns (google.protobuf.Empty) {
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

**AuthorityInfo**:

- **contract address**: new controller's contract address.
- **owner address**: new controller's address.

### GetMethodFee

This mehtod is used to query the method fee information.

```Protobuf
rpc GetMethodFee (google.protobuf.StringValue) returns (MethodFees) {
}

message MethodFees {
    string method_name = 1;
    repeated MethodFee fees = 2;
}
```

note: *for MethodFees see SetMethodFee*

### GetMethodFeeController

This mehtod is used to query the method fee information.

```Protobuf
rpc GetMethodFeeController (google.protobuf.Empty) returns (acs1.AuthorityInfo) { 
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

note: *for AuthorityInfo see ChangeMethodFeeController*

## view methods

For reference, you can find here the available view methods.

### GetFeeReceiverAddress

This method is used to query a the fee receiver.

```Protobuf
rpc GetFeeReceiverAddress (google.protobuf.Empty) returns (aelf.Address) {
}

message Address{
    bytes value = 1;
}
```

**returns**:

- **value** the receiver's address, the half fee is burned and the other half is transferred to receiver.

### GetControllerForManageConnector

This method is used to query the controller.

```Protobuf
rpc GetControllerForManageConnector (google.protobuf.Empty) returns (acs1.AuthorityInfo) {
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

note: *for AuthorityInfo see ChangeConnectorController*

### GetPairConnector

This method is used to get the connection information between the base token and other token, which inlcudes a pair connctors.

```Protobuf
rpc GetPairConnector (TokenSymbol) returns (PairConnector) {
}

message TokenSymbol {
    string symbol = 1;
}

message PairConnector{
    Connector resource_connector = 1;
    Connector deposit_connector = 2;
}
```

**TokenSymbol**:

- **symbol**: the token symbol.

**returns**:

- **resource connector**: the new add token connector.
- **deposit connector**: the corresponding base token connector.

note: *for Connector see Initialize*

### GetFeeRate

This method is used to query the fee rate.

```Protobuf
rpc GetFeeRate (google.protobuf.Empty) returns (google.protobuf.StringValue) {
}

message StringValue {
  string value = 1;
}
```

**returns**:

- **value**: the fee rate.

### GetBaseTokenSymbol

This method is used to query the base token symbol.

```Protobuf
rpc GetBaseTokenSymbol (google.protobuf.Empty) returns (TokenSymbol) {
}

message TokenSymbol {
  string symbol = 1;
}
```

**returns**:

- **symbol**: the token symbol.

### GetBaseGetNeededDeposit

This method is used to query how much the base token need be deposited before enabling the connectors.

```Protobuf
rpc GetNeededDeposit(ToBeConnectedTokenInfo) returns (DepositInfo) {
}

message ToBeConnectedTokenInfo{
    string token_symbol = 1;
    int64 amount_to_token_convert = 2;
}

message DepositInfo{
    int64 need_amount = 1;
    int64 amount_out_of_token_convert = 2;
}
```

**ToBeConnectedTokenInfo**:

- **token symbol**: the token symbol.
- **amount to token convert**: the added token amount you decide to transfer to TokenConvert.

**returns**:

- **need amount**: besides the amount you transfer to TokenConvert, how much base token you need deposit.
- **amount out of token convert**: how much the added token have not transferred to TokenConvert.

### GetDepositConnectorBalance

This method is used to query how much the base token have been deposited.

```Protobuf
rpc GetDepositConnectorBalance(google.protobuf.StringValue) returns (google.protobuf.Int64Value){
}

message StringValue {
  string value = 1;
}

message Int64Value {
  int64 value = 1;
}
```

**StringValue**:

- **value**: the token symbol.

**returns**:

- **value**: indicates for this token how much the base token have been deposited.

