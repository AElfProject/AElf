# vote

The Vote contract is most essentially used for voting for Block Producers.

## **Voting for Block Producers**:

For participating voting，user should register first. 

```Protobuf
rpc Register (VotingRegisterInput) returns (google.protobuf.Empty) {}

message VotingRegisterInput {
    google.protobuf.Timestamp start_timestamp = 1;
    google.protobuf.Timestamp end_timestamp = 2;
    string accepted_currency = 3;
    bool is_lock_token = 4;
    sint64 total_snapshot_number = 5;
    repeated string options = 6;
}
```

register requires a **VotingRegisterInput** message as parameter:
- **start timestamp** is start time for what
- **end timestamp** the end time for what
- **accepted currency** valid token can be used for voting
- **is lock token**  to do
- **total snapshot number** todo
- **options** todo


After registering successfully, user is ability to vote.

```Protobuf
rpc Vote (VoteInput) returns (google.protobuf.Empty) {}

message VoteInput {
    aelf.Hash voting_item_id = 1;
    aelf.Address voter = 2;
    sint64 amount = 3;
    string option = 4;
    aelf.Hash vote_id = 5;
    bool is_change_target = 6;
}
```
- **voting item id** hash of user to do.
- **voter**  address.
- **amount** amount you vote
- **option** todo
- **vote id** todo
- **is change targe** if it is your first time to vote, its value is setted defaultly to false
- **has context.fire;** 


if you regret to vote sb, you can withdraw your vote.

```Protobuf
rpc Withdraw (WithdrawInput) returns (google.protobuf.Empty) {}

message WithdrawInput {
    aelf.Hash vote_id = 1;
}
```

- **vote id**   vote id

Take snapshot.   todo

```Protobuf
rpc TakeSnapshot (TakeSnapshotInput) returns (google.protobuf.Empty) {}

message TakeSnapshotInput {
    aelf.Hash voting_item_id = 1;
    sint64 snapshot_number = 2;
}
```

- **voting item id** item id.
- **snapshot number**  NO.

title todo

```Protobuf
rpc AddOption (AddOptionInput) returns (google.protobuf.Empty) {}

message AddOptionInput {
    aelf.Hash voting_item_id = 1;
    string option = 2;
}
```

- **voting item id** item id.
- **option**  todo

title todo

```Protobuf
    rpc RemoveOption (RemoveOptionInput) returns (google.protobuf.Empty) {}

message RemoveOptionInput {
    aelf.Hash voting_item_id = 1;
    string option = 2;
}
```

- **voting item id** item id.
- **option**  todo

title todo

```Protobuf
rpc AddOptions (AddOptionsInput) returns (google.protobuf.Empty) {}

message AddOptionsInput {
    aelf.Hash voting_item_id = 1;
    repeated string options = 2;
}
```

- **voting item id** item id.
- **option**  todo


title todo

```Protobuf
rpc RemoveOptions (RemoveOptionsInput) returns (google.protobuf.Empty) {}

message RemoveOptionsInput {
    aelf.Hash voting_item_id = 1;
    repeated string options = 2;
}
```

- **voting item id** item id.
- **option**  todo

Change the manager of the token converter contract.

## view methods

For reference, you can find here the available view methods.

```Protobuf
    rpc GetTokenContractAddress (google.protobuf.Empty) returns (aelf.Address) {}
    rpc GetFeeReceiverAddress (google.protobuf.Empty) returns (aelf.Address) {}
    rpc GetManagerAddress (google.protobuf.Empty) returns (aelf.Address) {}
    rpc GetConnector (TokenSymbol) returns (Connector) {}
    rpc GetFeeRate (google.protobuf.Empty) returns (google.protobuf.StringValue) {}
    rpc GetBaseTokenSymbol (google.protobuf.Empty) returns (TokenSymbol) {}
```