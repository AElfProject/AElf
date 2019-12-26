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

Count the current term information by term number and save it

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


vote candidates

```Protobuf
rpc AddOptions (AddOptionsInput) returns (google.protobuf.Empty) {}

message AddOptionsInput {
    aelf.Hash voting_item_id = 1;
    repeated string options = 2;
}
```

- **voting item id** voting activity id
- **option** candidates' addresses




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




Remove candidates
```Protobuf
rpc RemoveOptions (RemoveOptionsInput) returns (google.protobuf.Empty) {}

message RemoveOptionsInput {
    aelf.Hash voting_item_id = 1;
    repeated string options = 2;
}
```

- **voting item id** voting activity id
- **option**   addresses of the candidates you want to remove

## view methods

For reference, you can find here the available view methods.


Get voting activity information
```Protobuf
rpc GetVotingItem (GetVotingItemInput) returns (VotingItem) {}

message GetVotingItemInput {
    aelf.Hash voting_item_id = 1;
}

message VotingItem {
    aelf.Hash voting_item_id = 1;
    string accepted_currency = 2;
    bool is_lock_token = 3;
    sint64 current_snapshot_number = 4;
    sint64 total_snapshot_number = 5;
    repeated string options = 6;
    google.protobuf.Timestamp register_timestamp = 7;
    google.protobuf.Timestamp start_timestamp = 8;
    google.protobuf.Timestamp end_timestamp = 9;
    google.protobuf.Timestamp current_snapshot_start_timestamp = 10;
    aelf.Address sponsor = 11;
}
```
GetVotingItemInput
- **voting item id** voting activity id

VotingItem
- **voting item id** voting activity id
- **accepted currency** vote token
- **is lock token** is token locked after voting
- **current snapshot number** current term
- **total snapshot number** total number of term
- **register timestamp** register time
- **start timestamp** start time
- **end timestamp** end time
- **current snapshot start timestamp** current term start time
- **sponsor** activity creator




Get voting result according to voting activity and term
```Protobuf
rpc GetVotingResult (GetVotingResultInput) returns (VotingResult) {}
 
message GetVotingResultInput {
    aelf.Hash voting_item_id = 1;
    sint64 snapshot_number = 2;
}

message VotingResult {
    aelf.Hash voting_item_id = 1;
    map<string, sint64> results = 2; // option -> amount
    sint64 snapshot_number = 3;
    sint64 voters_count = 4;
    google.protobuf.Timestamp snapshot_start_timestamp = 5;
    google.protobuf.Timestamp snapshot_end_timestamp = 6;
    sint64 votes_amount = 7;
}
```
GetVotingResultInput
- **voting item id** voting activity id
- **snapshot number** int which term ...

VotingResult
- **voting item id** voting activity id
- **results** candidate => votes
- **snapshot number** term number
- **voters count** how many voters
- **snapshot start timestamp** start time
- **snapshot end timestamp** end time
- **votes amount** total votes(excluding withdraws)



Get latest result according to voting activity
```Protobuf
rpc GetLatestVotingResult (aelf.Hash) returns (VotingResult) {}

message Hash
{
    bytes value = 1;
}

message VotingResult {
    aelf.Hash voting_item_id = 1;
    map<string, sint64> results = 2; // option -> amount
    sint64 snapshot_number = 3;
    sint64 voters_count = 4;
    google.protobuf.Timestamp snapshot_start_timestamp = 5;
    google.protobuf.Timestamp snapshot_end_timestamp = 6;
    sint64 votes_amount = 7;
}
```
Hash
- **value** voting activity id

VotingResult
- **voting item id** voting activity id
- **results** candidate => votes
- **snapshot number** term number
- **voters count** how many voters
- **snapshot start timestamp** start time
- **snapshot end timestamp** end time
- **votes amount** total votes(excluding withdraws)




Get voting record according to transaction id
```Protobuf
rpc GetVotingRecord (aelf.Hash) returns (VotingRecord) {}

message Hash
{
    bytes value = 1;
}
message VotingRecord {
    aelf.Hash voting_item_id = 1;
    aelf.Address voter = 2;
    sint64 snapshot_number = 3;
    sint64 amount = 4;
    google.protobuf.Timestamp withdraw_timestamp = 5;
    google.protobuf.Timestamp vote_timestamp = 6;
    bool is_withdrawn = 7;
    string option = 8;
    bool is_change_target = 9;
}
```

Hash
- **value** transaction id

VotingRecord
- **voting item id** voting activity id
- **voter** voter
- **snapshot number** term number
- **withdraw timestamp** withdraw time
- **vote timestamp**  vote time
- **is withdrawn**  has withdrawn
- **option**  candidate id
- **is change target**  has withdrawn and vote to others




Get voting records according to transaction ids
```Protobuf
rpc GetVotingRecords (GetVotingRecordsInput) returns (VotingRecords) {}


message GetVotingRecordsInput {
    repeated aelf.Hash ids = 1;
}

message Hash
{
    bytes value = 1;
}

message VotingRecords {
    repeated VotingRecord records = 1;
}

message VotingRecord {
    aelf.Hash voting_item_id = 1;
    aelf.Address voter = 2;
    sint64 snapshot_number = 3;
    sint64 amount = 4;
    google.protobuf.Timestamp withdraw_timestamp = 5;
    google.protobuf.Timestamp vote_timestamp = 6;
    bool is_withdrawn = 7;
    string option = 8;
    bool is_change_target = 9;
}

```
GetVotingRecordsInput
- **ids** transaction ids

Hash
- **value**  transaction id

VotingRecords
- **records**  records


VotingRecord
- **voting item id** voting activity id
- **voter** voter
- **snapshot number** term number
- **withdraw timestamp** withdraw time
- **vote timestamp**  vote time
- **is withdrawn**  has withdrawn
- **option**  candidate id
- **is change target**  has withdrawn and vote to others




Get voter's withdrawn and valid transaction ids respectively
```Protobuf
rpc GetVotedItems (aelf.Address) returns (VotedItems) {}

message Address
{
    bytes value = 1;
}

message VotedItems {
    map<string, VotedIds> voted_item_vote_ids = 1;
}

message VotedIds {
    repeated aelf.Hash active_votes = 1;
    repeated aelf.Hash withdrawn_votes = 2;
}
```
Address
- **value** voter address

VotedItems
- **voted item vote ids**  voting activity => vote information

VotedIds
- **active votes** valid transaction id
- **withdrawn votes**withdrawn transaction id





Get voter's withdrawn and valid transaction ids respectively according to voting activity
```Protobuf
rpc GetVotingIds (GetVotingIdsInput) returns (VotedIds) {}

message GetVotingIdsInput {
    aelf.Address voter = 1;
    aelf.Hash voting_item_id = 2;
}

message VotedIds {
    repeated aelf.Hash active_votes = 1;
    repeated aelf.Hash withdrawn_votes = 2;
}
```
GetVotingIdsInput
- **voter** voter address
- **voting item id** voting activity id


VotedIds
- **active votes** valid transaction id
- **withdrawn votes**withdrawn transaction id


