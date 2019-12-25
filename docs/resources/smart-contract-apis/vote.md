# Vote Contract

The Vote contract is an abstract layer for voting. Developers  implement concrete voting activity by call this contract.

## **Voting for Block Producers**:

To build a voting activity，the developer should register first. 

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
- **start timestamp** activity start time
- **end timestamp** activity end time
- **accepted currency** indicate which token is accepted
- **is lock token**  whether lock token 
- **total snapshot number** number of terms
- **options** default candidate


After the developer successfully build a voting activity, users are able to vote.

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

message Voted {
    option (aelf.is_event) = true;
    aelf.Hash voting_item_id = 1;
    aelf.Address voter = 2;
    sint64 snapshot_number = 3;
    sint64 amount = 4;
    google.protobuf.Timestamp vote_timestamp = 5;
    string option = 6;
    aelf.Hash vote_id = 7;
}
```
- **voting item id** indicate which voting activity the user participate in
- **voter**  voter address.
- **amount** amount you vote
- **option** candidate public key
- **vote id** transaction id
- **is change target** indicate whether you changed option

- **has context.fire;** 
Voted
- **voting item id**  voting activity id
- **voter** voter address
- **snapshot number** indicate current term
- **amount**amount you vote
- **vote timestamp**vote time
- **option** the candidate's public key
- **vote id**transaction id

if you regret to vote sb, you can withdraw your vote.

```Protobuf
rpc Withdraw (WithdrawInput) returns (google.protobuf.Empty) {}

message WithdrawInput {
    aelf.Hash vote_id = 1;
}
```

- **vote id**   transaction id

Statistic the current term information by term number and save

```Protobuf
rpc TakeSnapshot (TakeSnapshotInput) returns (google.protobuf.Empty) {}

message TakeSnapshotInput {
    aelf.Hash voting_item_id = 1;
    sint64 snapshot_number = 2;
}
```

- **voting item id** voting activity id.
- **snapshot number**  the term number



vote a new candidate

```Protobuf
rpc AddOption (AddOptionInput) returns (google.protobuf.Empty) {}

message AddOptionInput {
    aelf.Hash voting_item_id = 1;
    string option = 2;
}
```

- **voting item id** voting activity id
- **option**  the new candidate address


Remove a candidate you voted
```Protobuf
    rpc RemoveOption (RemoveOptionInput) returns (google.protobuf.Empty) {}

message RemoveOptionInput {
    aelf.Hash voting_item_id = 1;
    string option = 2;
}
```

- **voting item id** voting activity id
- **option**   address of the candidate you want to remove


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