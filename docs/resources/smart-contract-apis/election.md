# election

The Election contract is most essentially used for voting for Block Producers.

## **an Election for choosing Block Producers**:

To be a Block Producer, user should register to be a candidate first. Besides, as a candidate, you should pay a 100000 ELF deposit, and will get 1 weight(10 weight limited) for sharing bonus in the future.

```Protobuf
rpc AnnounceElection (google.protobuf.Empty) returns (google.protobuf.Empty) {}
```


Although you have been a candidate, you are able to quit the election given that you are not a miner. If you quit successfully, you can get your deposite back.

```Protobuf
rpc QuitElection (google.protobuf.Empty) returns (google.protobuf.Empty) {}
```

Vote a candidate to be elected. The token you vote will be locked till the end time. According to the number of token you voted and their lock time, you can get corresponding weight for sharing the bonus in the future.
```Protobuf
rpc Vote (VoteMinerInput) returns (google.protobuf.Empty) {}

message VoteMinerInput {
    string candidate_pubkey = 1;
    sint64 amount = 2;
    google.protobuf.Timestamp end_timestamp = 3;
}
```

- **candidate pubkey**   candidate id
- **amount**   amount token to vote
- **end timestamp**  before which, your vote works.


Before the end time, you can change your vote to other candidate.
```Protobuf
rpc ChangeVotingOption (ChangeVotingOptionInput) returns google.protobuf.Empty){}

message TakeSnapshotInput {
    aelf.Hash voting_item_id = 1;
    sint64 snapshot_number = 2;
}
```

- **voting item id** item id.
- **snapshot number**  NO.

Withdraw todo

```Protobuf
rpc Withdraw (aelf.Hash) returns (google.protobuf.Empty) {}

message Hash
{
    bytes value = 1;
}
```

- **value** hash id.

## **an Election for choosing Block Producers (Views)**:

GetCandidates

```Protobuf
rpc GetCandidates (google.protobuf.Empty) returns (PubkeyList) {}

message PubkeyList {
    repeated bytes value = 1;
}
```

- **value** hash id of all candidate.

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