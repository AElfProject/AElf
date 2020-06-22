# TokenConvert Contract

Using this contract can build a connection between the base token(the default is native token) and other tokens created on the chain. After building the connection, users can trade tokens with the Bancor model. You can find the detail information about Bancor in AElf Economic System White Paper.

## **Actions**

### **InitializeInput**

```Protobuf
rpc Initialize (InitializeInput) returns (google.protobuf.Empty){}

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

This method is used to initialize this contract (add base token, connections, etc.).

- **InitializeInput**
  - **base token symbol**: base token, default is the native token.
  - **fee rate**: buy/sell token need pay the fee( = cost * feeRate).
  - **connectors**: the default added connectors.

We use Bancor model to build the connection between base token and other tokens. Each pair connectors include the coefficients used by calculating the token price based on the base token, and it consists of the base token connector and the new token connector.

- **Connector**
  - **symbol**: the connector symbol.
  - **related symbol**: indicates its related connector, the pair connector includes a new created token connector and the base token connector.
  - **virtual balance**: used in bancor model.
  - **weight**: the weight is linked to the related connector's weight.
  - **is virtual balance enabled**: true indicates that the virtual balance is used in price calculation.
  - **is purchase enabled**: after build a pair connector, you can not buy/sell the token immediately, the default is false.
  - **is deposit account**: indicates if the connector is base token connector.

### **AddPairConnector**

```Protobuf
rpc AddPairConnector(PairConnectorParam) returns (google.protobuf.Empty){}

message PairConnectorParam {
    string resource_connector_symbol = 1;
    string resource_weight = 2;
    int64 native_virtual_balance = 3;
    string native_weight = 4;
}
```

With Bancor model, each new token need a pair connectors to calculate its price. Only the connector contoller(the default is parliament) is allowed to call this API.

- **PairConnectorParam**
  - **resource connector symbol**: the new token connector's symbol.
  - **resource weight**: the new token connector's weight.
  - **native virtual balance**: the related base token connector's virtual balance.
  - **native weight**: base token's weight.

### **EnableConnector**

```Protobuf
rpc EnableConnector (ToBeConnectedTokenInfo) returns (google.protobuf.Empty){}

message ToBeConnectedTokenInfo{
    string token_symbol = 1;
    int64 amount_to_token_convert = 2;
}
```

To make the connection work, the connector contoller need send this transaction.

- **ToBeConnectedTokenInfo**
  - **token symbol**: the token symbol.
  - **amount to token convert**: to make the token trade, maybe you need deposit some base token.

### **UpdateConnector**

```Protobuf
rpc UpdateConnector(Connector) returns (google.protobuf.Empty){}
```

Before calling the EnableConnector, the connector contoller update the pair connctors' information.

note: *for Connector see Initialize*

### **Buy**

```Protobuf
rpc Buy (BuyInput) returns (google.protobuf.Empty){}

message BuyInput {
    string symbol = 1;
    int64 amount = 2;
    int64 pay_limit = 3;
}
```

After building the connection and enabling it, you can buy the new token with the base token.

- **BuyInput**
  - **symbol**: the token symbol.
  - **amount**: the amount you want to buy.
  - **pay limit**: no buy if paying more than this, 0 if no limit.

### **Sell**

```Protobuf
rpc Sell (SellInput) returns (google.protobuf.Empty){}

message SellInput {
    string symbol = 1;
    int64 amount = 2;
    int64 receive_limit = 3;
}
```

After building the connection and enabling it, you can sell the new token.

- **SellInput**
  - **symbol**: the token symbol.
  - **amount**: the amount you want to sell.
  - **receive limit**: no sell if receiving less than this, 0 if no limit.

### **ChangeConnectorController**

```Protobuf
rpc ChangeConnectorController (acs1.AuthorityInfo) returns (google.protobuf.Empty){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

The controller can be transferred to others.

note: *for AuthorityInfo see ChangeMethodFeeController*

## **Acs1 specific methods**

For reference, you can find here the methods implementing acs1.

### SetMethodFee

```Protobuf
rpc SetMethodFee (MethodFees) returns (google.protobuf.Empty){}

message MethodFees {
    string method_name = 1;
    repeated MethodFee fees = 2;
}

message MethodFee {
    string symbol = 1;
    int64 basic_fee = 2;
}
```

It sets method fee.

- **MethodFees**
  - **method name**: the method name in this contract.
  - **fees**: fee list.

- **MethodFee**
  - **symbol** token symbol.
  - **basic fee** fee.

### ChangeMethodFeeController

```Protobuf
rpc ChangeMethodFeeController (AuthorityInfo) returns (google.protobuf.Empty){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Change the method fee controller, the default is Parliament.

- **AuthorityInfo**
  - **contract address**: new controller's contract address.
  - **owner address**: new controller's address.

### GetMethodFee

```Protobuf
rpc GetMethodFee (google.protobuf.StringValue) returns (MethodFees){}

message MethodFees {
    string method_name = 1;
    repeated MethodFee fees = 2;
}
```

This mehtod is used to query the method fee information.

note: *for MethodFees see SetMethodFee*

### GetMethodFeeController

```Protobuf
rpc GetMethodFeeController (google.protobuf.Empty) returns (acs1.AuthorityInfo){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

This mehtod is used to query the controller of the method fee.

note: *for AuthorityInfo see ChangeMethodFeeController*

## **View methods**

For reference, you can find here the available view methods.

### GetFeeReceiverAddress

```Protobuf
rpc GetFeeReceiverAddress (google.protobuf.Empty) returns (aelf.Address){}

message Address{
    bytes value = 1;
}
```

This method is used to query the fee receiver.

- **Returns**
  - **value** the receiver's address, the half fee is burned and the other half is transferred to receiver.

### GetControllerForManageConnector

```Protobuf
rpc GetControllerForManageConnector (google.protobuf.Empty) returns (acs1.AuthorityInfo){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

his method is used to query the controller of the connector.

note: *for AuthorityInfo see ChangeConnectorController*

### GetPairConnector

```Protobuf
rpc GetPairConnector (TokenSymbol) returns (PairConnector){}

message TokenSymbol {
    string symbol = 1;
}

message PairConnector{
    Connector resource_connector = 1;
    Connector deposit_connector = 2;
}
```

This method is used to get the connection information between the base token and other token, which inlcudes a pair connctors.

- **TokenSymbol**
  - **symbol**: the token symbol.

- **Returns**
  - **resource connector**: the new add token connector.
  - **deposit connector**: the corresponding base token connector.

note: *for Connector see Initialize*

### GetFeeRate

```Protobuf
rpc GetFeeRate (google.protobuf.Empty) returns (google.protobuf.StringValue){}

message StringValue {
  string value = 1;
}
```

This method is used to query the fee rate.

- **Returns**
  - **value**: the fee rate.

### GetBaseTokenSymbol

```Protobuf
rpc GetBaseTokenSymbol (google.protobuf.Empty) returns (TokenSymbol){}

message TokenSymbol {
  string symbol = 1;
}
```

This method is used to query the base token symbol.

- **Returns**:
  - **symbol**: the token symbol.

### GetBaseGetNeededDeposit

```Protobuf
rpc GetNeededDeposit(ToBeConnectedTokenInfo) returns (DepositInfo){}

message ToBeConnectedTokenInfo{
    string token_symbol = 1;
    int64 amount_to_token_convert = 2;
}

message DepositInfo{
    int64 need_amount = 1;
    int64 amount_out_of_token_convert = 2;
}
```

This method is used to query how much the base token need be deposited before enabling the connectors.

- **ToBeConnectedTokenInfo**
  - **token symbol**: the token symbol.
  - **amount to token convert**: the added token amount you decide to transfer to TokenConvert.

- **Returns**
  - **need amount**: besides the amount you transfer to TokenConvert, how much base token you need deposit.
  - **amount out of token convert**: how much the added token have not transferred to TokenConvert.

### GetDepositConnectorBalance

```Protobuf
rpc GetDepositConnectorBalance(google.protobuf.StringValue) returns (google.protobuf.Int64Value){}

message StringValue {
  string value = 1;
}

message Int64Value {
  int64 value = 1;
}
```

This method is used to query how much the base token have been deposited.

- **StringValue**:
  - **value**: the token symbol.

- **Returns**:
  - **value**: indicates for this token how much the base token have been deposited.
