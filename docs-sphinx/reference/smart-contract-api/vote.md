# Vote Contract

The Vote contract is an abstract layer for voting. Developers implement concrete voting activities by calling this contract.

## **Actions**

### **Voting for Block Producers**

```Protobuf
rpc Register (VotingRegisterInput) returns (google.protobuf.Empty){}

message VotingRegisterInput {
    google.protobuf.Timestamp start_timestamp = 1;
    google.protobuf.Timestamp end_timestamp = 2;
    string accepted_currency = 3;
    bool is_lock_token = 4;
    sint64 total_snapshot_number = 5;
    repeated string options = 6;
}
```

To build a voting activity, the developer should register first.

- **VotingRegisterInput**
  - **start timestamp**: activity start time.
  - **end timestamp**: activity end time.
  - **accepted currency**: the token symbol which will be accepted.
  - **is lock token**: indicates whether the token will be locked after voting. 
  - **total snapshot number**: number of terms.
  - **options**: default candidate.

### **Vote**

```Protobuf
rpc Vote (VoteInput) returns (google.protobuf.Empty){}

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

After building successfully a voting activity, others are able to vote.

- **VoteInput**
  - **voting item id**: indicates which voting activity the user participate in.
  - **voter**: voter's address.
  - **amount**: vote amount.
  - **option**: candidate's public key.
  - **vote id**: transaction id.
  - **is change target**: indicates whether the option is changed.

- **Event**
  - **Voted**
    - **voting item id**: voting activity id.
    - **voter**: voter's address.
    - **snapshot number**: the current round.
    - **amount**: vote amount.
    - **vote timestamp**: vote time.
    - **option**: the candidate's public key.
    - **vote id**: transaction id.

### **Withdraw**

```Protobuf
rpc Withdraw (WithdrawInput) returns (google.protobuf.Empty){
}

message WithdrawInput {
    aelf.Hash vote_id = 1;
}

message Withdrawn {
    aelf.Hash vote_id = 1;
}
```

A voter can withdraw the token after the lock time.

- **WithdrawInput**
  - **vote id**: transaction id.

- **Event**
  - **Withdrawn**
    - **vote id**: transaction id.

### **TakeSnapshot**

```Protobuf
rpc TakeSnapshot (TakeSnapshotInput) returns (google.protobuf.Empty){}

message TakeSnapshotInput {
    aelf.Hash voting_item_id = 1;
    sint64 snapshot_number = 2;
}
```

Distributes profits and saves the state every round.

- **TakeSnapshotInput**
  - **voting item id**: voting activity id.
  - **snapshot number**: the round number.

### **AddOption**

```Protobuf
rpc AddOption (AddOptionInput) returns (google.protobuf.Empty){
}

message AddOptionInput {
    aelf.Hash voting_item_id = 1;
    string option = 2;
}
```

Adds an option (a choice) to a voting activity.

- **AddOptionInput**
  - **voting item id**: vote activity id.
  - **option**: the new option.

### **AddOptions**

```Protobuf
rpc AddOptions (AddOptionsInput) returns (google.protobuf.Empty){
}

message AddOptionsInput {
    aelf.Hash voting_item_id = 1;
    repeated string options = 2;
}
```

Adds multiple options (choices) to a voting activity.

- **AddOptionsInput**
  - **voting item id**: voting activity id.
  - **option**: the list of new options.

### **RemoveOption**

```Protobuf
rpc RemoveOption (RemoveOptionInput) returns (google.protobuf.Empty){  
}

message RemoveOptionInput {
    aelf.Hash voting_item_id = 1;
    string option = 2;
}
```

Removes an option from a voting activity.

- **RemoveOptionInput**
  - **voting item id**: voting activity id.
  - **option**: the option to remove.

### **RemoveOptions**

```Protobuf
rpc RemoveOptions (RemoveOptionsInput) returns (google.protobuf.Empty){}

message RemoveOptionsInput {
    aelf.Hash voting_item_id = 1;
    repeated string options = 2;
}
```

Removes multiple options from a voting activity.

- **RemoveOptionsInput**
  - **voting item id**: voting activity id.
  - **option**: the options to remove.

## **View methods**

For reference, you can find here the available view methods.

### GetVotingItem

```Protobuf
rpc GetVotingItem (GetVotingItemInput) returns (VotingItem){
}

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

Gets the information related to a voting activity.

- **GetVotingItemInput**
  - **voting item id**: voting activity id.

- **Returns**
  - **voting item id**: voting activity id.
  - **accepted currency**: vote token.
  - **is lock token**: indicates if the token will be locked after voting.
  - **current snapshot number**: current round.
  - **total snapshot number**: total number of round.
  - **register timestamp**: register time.
  - **start timestamp**: start time.
  - **end timestamp**: end time.
  - **current snapshot start timestamp**: current round start time.
  - **sponsor**: activity creator.

### GetVotingResult

```Protobuf
rpc GetVotingResult (GetVotingResultInput) returns (VotingResult){}

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

Gets a voting result according to the provided voting activity id and round number.

- **GetVotingResultInput**
  - **voting item id**: voting activity id.
  - **snapshot number**: round number.

- **Returns**:
  - **voting item id**: voting activity id.
  - **results**: candidate => vote amount.
  - **snapshot number**: round number.
  - **voters count**: how many voters.
  - **snapshot start timestamp**: start time.
  - **snapshot end timestamp**: end time.
  - **votes amount** total votes(excluding withdraws).

### GetLatestVotingResult

```Protobuf
rpc GetLatestVotingResult (aelf.Hash) returns (VotingResult){}

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

Gets the latest result of the provided voting activity.

- **Hash**
  - **value**: voting activity id.

- **Returns**
  - **voting item id**: voting activity id.
  - **results**: candidate => vote amount.
  - **snapshot number**: round number.
  - **voters count**: how many voters.
  - **snapshot start timestamp**: start time.
  - **snapshot end timestamp**: end time.
  - **votes amount**: total votes(excluding withdraws).

### GetVotingRecord

```Protobuf
rpc GetVotingRecord (aelf.Hash) returns (VotingRecord){
}

message Hash{
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

Get the voting record for the given record ID.

- **Hash**
  - **value**: transaction id.

- **Returns**
  - **voting item id**: voting activity id.
  - **voter**: voter's address.
  - **snapshot number**: round number.
  - **withdraw timestamp**: withdraw time.
  - **vote timestamp**: vote time.
  - **is withdrawn**: indicate whether the vote has been withdrawn.
  - **option**: candidate id.
  - **is change target**: has withdrawn and vote to others.

### GetVotingRecords

```Protobuf
rpc GetVotingRecords (GetVotingRecordsInput) returns (VotingRecords){}

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

Get the voting records for the given record IDs.

- **GetVotingRecordsInput**
  - **ids**: transaction ids.

- **Hash**
  - **value**: transaction id.

- **Returns**
  - **records**: records.

- **VotingRecord**
  - **voting item id**: voting activity id.
  - **voter**: voter's address.
  - **snapshot number**: round number.
  - **withdraw timestamp**: withdraw time.
  - **vote timestamp**: vote time.
  - **is withdrawn**: indicates whether the vote has been withdrawn.
  - **option**: candidate id.
  - **is change target**: has withdrawn and vote to others.

### GetVotedItems

```Protobuf
rpc GetVotedItems (aelf.Address) returns (VotedItems){
}

message Address{
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

Get the voter's withdrawn and valid transaction ids respectively.

- **Address**
  - **value**: voter's address.

- **Returns**
  - **voted item vote ids**: voting activity id => vote information.

- **VotedIds**
  - **active votes**: valid transaction id.
  - **withdrawn votes**: withdrawn transaction id.

### GetVotingIds

```Protobuf
rpc GetVotingIds (GetVotingIdsInput) returns (VotedIds){
}

message GetVotingIdsInput {
    aelf.Address voter = 1;
    aelf.Hash voting_item_id = 2;
}

message VotedIds {
    repeated aelf.Hash active_votes = 1;
    repeated aelf.Hash withdrawn_votes = 2;
}
```

Get the voter's withdrawn and valid transaction ids respectively according to voting activity id.

- **GetVotingIdsInput**
  - **voter**: voter's address.
  - **voting item id**: voting activity id.

- **Returns**
  - **active votes**: valid transaction id.
  - **withdrawn votes**: withdrawn transaction id.
  