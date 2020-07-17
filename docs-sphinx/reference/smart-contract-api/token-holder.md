# Token Holder Contract

The TokenHolder contract is essentially used for building a bouns model for distributing bonus' to whom hold the token.

## **Actions**

### **CreateScheme**

```Protobuf
rpc CreateScheme (CreateTokenHolderProfitSchemeInput) returns (google.protobuf.Empty){}

message CreateTokenHolderProfitSchemeInput {
    string symbol = 1;
    int64 minimum_lock_minutes = 2;
    map<string, int64> auto_distribute_threshold = 3;
}
```

It creates a scheme which stores relevant information about dividend in profit contract.

- **CreateTokenHolderProfitSchemeInput**
  - **symbol**: the token that will be used for locking and distributing profits.
  - **minimum lock time** : minimum lock time before withdrawing.
  - **automatic distribution threshold**: used when registering for profits (RegisterForProfits).

### **AddBeneficiary**

```Protobuf
rpc AddBeneficiary (AddTokenHolderBeneficiaryInput) returns (google.protobuf.Empty){}

message AddTokenHolderBeneficiaryInput {
    aelf.Address beneficiary = 1;
    int64 shares = 2;
}
```

Add a Beneficiary to a scheme.

- **AddTokenHolderBeneficiaryInput**
  - **beneficiary**: the new beneficiary.
  - **shares**: the shares to attribute to this beneficiary.

### **RemoveBeneficiary**

```Protobuf
rpc RemoveBeneficiary (RemoveTokenHolderBeneficiaryInput) returns (google.protobuf.Empty){}

message RemoveTokenHolderBeneficiaryInput {
    aelf.Address beneficiary = 1;
    int64 amount = 2;
}
```

Remove a Beneficiary from a scheme.
Note: this method can be used to remove a beneficiary or update its shares.

- **RemoveTokenHolderBeneficiaryInput**
  - **beneficiary**: the beneficiary to remove or update.
  - **amount**: 0 to remove the beneficiary. A positive integer, smaller than the current shares.

### **ContributeProfits**

```Protobuf
rpc ContributeProfits (ContributeProfitsInput) returns (google.protobuf.Empty){}

message ContributeProfitsInput {
    aelf.Hash scheme_id = 1;
    int64 amount = 2;
    int64 period = 3;
    string symbol = 4;
}
```

Contribute some token to a scheme.

- **ContributeProfitsInput**
  - **scheme id**: scheme id you want to contribute.
  - **amount**: the amount of tokens to contribute.
  - **period**: the period you want to contribute.
  - **symbol**: the token to contribute.

### **DistributeProfits**

```Protobuf
rpc DistributeProfits (DistributeProfitsInput) returns (google.protobuf.Empty){}

message DistributeProfitsInput {
    aelf.Address scheme_manager = 1;
    string symbol = 2;
}
```

Distribute a profit plan, which means its beneficiaries are going to get the shares.

- **DistributeProfitsInput**
  - **scheme manager**: manager of the scheme; when creating the scheme the Sender is set to manager. 
  - **symbol**: the token to contribute.

### **RegisterForProfits**

```Protobuf
rpc RegisterForProfits (RegisterForProfitsInput) returns (google.protobuf.Empty){}

message RegisterForProfitsInput {
    aelf.Address scheme_manager = 1;
    int64 amount = 2;
}
```

- **RegisterForProfitsInput**
  - **scheme manager**: manager of the scheme; when creating the scheme the Sender is set to manager.
  - **amount**: the amount of tokens to lock (and will correspond to the amount of shares).

### **Withdraw**

```Protobuf
rpc Withdraw (aelf.Address) returns (google.protobuf.Empty){}
```

This method will withdraw the given address for the Token Holder contract, this will also unlock the previously locked tokens.

### **ClaimProfits**

```Protobuf
rpc ClaimProfits (ClaimProfitsInput) returns (google.protobuf.Empty){}

message ClaimProfitsInput {
    aelf.Address scheme_manager = 1;
    aelf.Address beneficiary = 2;
}
```

The sender withdraws his/her token from the scheme.

- **ClaimProfitsInput**
  - **scheme manager**: manager of the scheme; when creating the scheme the Sender is set to manager.
  - **beneficiary**: the beneficiary, defaults to the Sender.

## **View methods**

### **GetScheme**

```Protobuf
rpc GetScheme (aelf.Address) returns (TokenHolderProfitScheme) { }

message TokenHolderProfitScheme {
    string symbol = 1;
    aelf.Hash scheme_id = 2;
    int64 period = 3;
    int64 minimum_lock_minutes = 4;
    map<string, int64> auto_distribute_threshold = 5;
}
```

Returns a description of the scheme, wrapped in a **TokenHolderProfitScheme** object:

- **Returns**
  - **symbol**: the scheme's token.
  - **scheme id**: the id of the scheme.
  - **period**: the current period of the scheme.
  - **minimum lock minutes**: minimum lock time.
  - **automatic distribution threshold**: distribution info.
