# resource

The TokenConverter contract is most essentially used for managing resources.

## **Buying and selling resources**:

The token converter's contract permits buying and selling resource based on the **Bancor** algorithm.

```Protobuf
rpc Buy (BuyInput) returns (google.protobuf.Empty) {}
rpc Sell (SellInput) returns (google.protobuf.Empty) {}

message BuyInput {
    string symbol = 1;
    sint64 amount = 2;
    sint64 pay_limit = 3;
}

message SellInput {
    string symbol = 1;
    sint64 amount = 2;
    sint64 receive_limit = 3;
}

// Events
message TokenBought {
    option (aelf.is_event) = true;
    string symbol = 1 [(aelf.is_indexed) = true];
    sint64 bought_amount = 2;
    sint64 base_amount = 3;
    sint64 fee_amount = 4;
}

message TokenSold {
    option (aelf.is_event) = true;
    string symbol = 1 [(aelf.is_indexed) = true];
    sint64 sold_amount = 2;
    sint64 base_amount = 3;
    sint64 fee_amount = 4;
}
```

Buying resource requires a **BuyInput** message as parameter:
- **symbol** is token symbol to buy, it effectively describes which connector will be used.
- **amount** the amount of tokens to buy.
- **pay limit** is used to cap the amount of that you are willing to pay for this exchange (0 for no limit).

After a successful buy, a **TokenBought** event log can be found in the transaction result.

On the contrary when selling resources you need to send a **SellInput** message as parameter:
- **symbol** is token symbol to sell.
- **amount** the amount of tokens to sell.
- **receive limit** is the minimum amount of tokens that you are willing to receive for this exchange (0 for no limit).

After a successful sell, a **TokenSold** event log can be found in the transaction result.

**Contract management**:

These next methods for managing the token converter contract are only accessible by the manager (the initial parliament organization).

```Protobuf
rpc SetConnector (Connector) returns (google.protobuf.Empty) {}

message Connector {
    string symbol = 1;
    sint64 virtual_balance = 2;
    string weight = 3;
    bool is_virtual_balance_enabled = 4;
    bool is_purchase_enabled = 5;
}
```

This method will add a connector for the given symbol.
- **symbol** the target symbol of the connector.
- **weight** is a decimal that has to be a decimal between 0 and 1.
- **virtual_balance** and **is_virtual_balance_enabled** control what balance is used for the buy and sell operations. 

```Protobuf
rpc SetFeeRate (google.protobuf.StringValue) returns (google.protobuf.Empty) {}
```

Sets the fee rate, a string formatted decimal between 0 and 1. 

```Protobuf
rpc SetManagerAddress (aelf.Address) returns (google.protobuf.Empty) {}
```

Change the manager of the token converter contract.

## view methods

For reference, you can find here the available view methods.

```Protobuf
rpc GetFeeReceiverAddress (google.protobuf.Empty) returns (aelf.Address) {}
rpc GetManagerAddress (google.protobuf.Empty) returns (aelf.Address) {}
rpc GetConnector (TokenSymbol) returns (Connector) {}
rpc GetFeeRate (google.protobuf.Empty) returns (google.protobuf.StringValue) {}
rpc GetBaseTokenSymbol (google.protobuf.Empty) returns (TokenSymbol) {}
```