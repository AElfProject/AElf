# Election Contract

The Election contract is essentially used for voting for Block Producers.

## **Actions**

### **AnnounceElection**

```Protobuf
rpc AnnounceElection (google.protobuf.Empty) returns (google.protobuf.Empty){}
```

To be a block producer, a user should first register to be a candidate and lock some token as a deposit. If the data center is not full, the user will be added in automatically and get one weight (10 weight limited) for sharing bonus in the future.

### **QuitElection**

```Protobuf
rpc QuitElection (google.protobuf.Empty) returns (google.protobuf.Empty){}
```

A candidate is able to quit the election provided he is not currently elected. If you quit successfully, the candidate will get his locked tokens back and will not receive anymore bonus.

### **Vote**

```Protobuf
rpc Vote (VoteMinerInput) returns (aelf.Hash){}

message VoteMinerInput {
    string candidate_pubkey = 1;
    sint64 amount = 2;
    google.protobuf.Timestamp end_timestamp = 3;
}

message Hash
{
    bytes value = 1;
}
```

Used for voting for a candidate to be elected. The tokens you vote with will be locked until the end time. According to the number of token you voted and its lock time, you can get corresponding weight for sharing the bonus in the future.

- **VoteMinerInput**
  - **candidate pubkey**: candidate public key.
  - **amount**: amount token to vote.
  - **end timestamp**: before which, your vote works.

- **Returns**
  - **value**: vote id.

### **ChangeVotingOption**

```Protobuf
rpc ChangeVotingOption (ChangeVotingOptionInput) returns google.protobuf.Empty){}

message ChangeVotingOptionInput {
    aelf.Hash vote_id = 1;
    string candidate_pubkey = 2;
}
```

Before the end time, you are able to change your vote target to other candidates.

- **ChangeVotingOptionInput**
  - **voting vote id**: transaction id.
  - **candidate pubkey**: new candidate public key.

### **Withdraw**

```Protobuf
rpc Withdraw (aelf.Hash) returns (google.protobuf.Empty){}

message Hash
{
    bytes value = 1;
}
```

After the lock time, your locked tokens will be unlocked and you can withdraw them.

- **Hash**
  - **value**: transaction id.

### **SetVoteWeightProportion**

```Protobuf
rpc SetVoteWeightProportion (VoteWeightProportion) returns (google.protobuf.Empty){
}

message VoteWeightProportion {
    int32 time_proportion = 1;
    int32 amount_proportion = 2;
}
```

Vote weight calcualtion takes in consideration the amount you vote and the lock time your vote.

- **VoteWeightProportion**
  - **time proportion**: time's weight.
  - **amount proportion**: amount's weight.

## **View methods**

For reference, you can find here the available view methods.

### GetCandidates

```Protobuf
rpc GetCandidates (google.protobuf.Empty) returns (PubkeyList){
}

message PubkeyList {
    repeated bytes value = 1;
}
```

Gets all candidates' public keys.

- **Returns**
  - **value** public key array of candidates

### GetVotedCandidates

```Protobuf
rpc GetVotedCandidates (google.protobuf.Empty) returns (PubkeyList){
}

message PubkeyList {
    repeated bytes value = 1;
}
```

Gets all candidates whose number of votes is greater than 0.

- **Returns**
  - **value** public key array of candidates.

### GetCandidateInformation

```Protobuf
rpc GetCandidateInformation (google.protobuf.StringValue) returns (CandidateInformation){}

message StringValue {
  string value = 1;
}

message CandidateInformation {
    string pubkey = 1;
    repeated sint64 terms = 2;
    sint64 produced_blocks = 3;
    sint64 missed_time_slots = 4;
    sint64 continual_appointment_count = 5;
    aelf.Hash announcement_transaction_id = 6;
    bool is_current_candidate = 7;
}
```

Gets a candidate's information. If the candidate does not exist, it will return a candidate without any information.

- **StringValue**
  - **value**: public key (hexadecimal string) of the candidate.

- **Returns**
  - **pubkey**: public key (represented by an hexadecimal string).
  - **terms**: indicates in which terms the candidate participated.
  - **produced blocks**: the number of blocks the candidate has produced. 
  - **missed time slots**: the time slot for which the candidate failed to produce blocks.
  - **continual appointment count**: the time the candidate continue to participate in the election.
  - **announcement transaction id**: the transaction id that the candidate announce.
  - **is current candidate**: indicate whether the candidate can be elected in the current term.

### GetVictories

```Protobuf
rpc GetVictories (google.protobuf.Empty) returns (PubkeyList){}

message PubkeyList {
    repeated bytes value = 1;
}
```

Gets the victories of the latest term.

- **Returns**
  - **value** the array of public key who has been elected as block producers.

### GetTermSnapshot

```Protobuf
rpc GetTermSnapshot (GetTermSnapshotInput) returns (TermSnapshot){}

message GetTermSnapshotInput {
    sint64 term_number = 1;
}

message TermSnapshot {
    sint64 end_round_number = 1;
    sint64 mined_blocks = 2;
    map<string, sint64> election_result = 3;
}
```

Gets the snapshot of the term provided as input.

- **GetTermSnapshotInput**
  - **term number**: term number.

- **Returns**
  - **end round number**: the last term id be saved.
  - **mined blocks**: number of blocks produced in previous term.
  - **election result**: candidate => votes.

### GetMinersCount

```Protobuf
rpc GetMinersCount (google.protobuf.Empty) returns (aelf.SInt32Value){}

message SInt32Value
{
    sint32 value = 1;
}
```

Count miners.

- **Returns**
  - **value**: the total number of block producers.

### GetElectionResult

```Protobuf
rpc GetElectionResult (GetElectionResultInput) returns (ElectionResult){}

message GetElectionResultInput {
    sint64 term_number = 1;
}

message ElectionResult {
    sint64 term_number = 1;
    map<string, sint64> results = 2;
    bool is_active = 3;
}
```

Gets an election result by term id.

- **GetElectionResultInput**
  - **term number**: term id.

- **Returns**:
  - **term number**: term id.
  - **results**: candidate => votes.
  - **is active**: indicates that if the term number you input is the current term.

### GetElectorVote

```Protobuf
rpc GetElectorVote (google.protobuf.StringValue) returns (ElectorVote){}

message StringValue {
  string value = 1;
}

message ElectorVote {
    repeated aelf.Hash active_voting_record_ids = 1;// Not withdrawn.
    repeated aelf.Hash withdrawn_voting_record_ids = 2;
    sint64 active_voted_votes_amount = 3;
    sint64 all_voted_votes_amount = 4;
    repeated ElectionVotingRecord active_voting_records = 5;
    repeated ElectionVotingRecord withdrawn_votes_records = 6;
    bytes pubkey = 7;
}
```

Gets the voter's information.

- **StringValue**
  - **value**: the public key (hexadecimal string) of voter.

- **Returns**
  - **active voting record ids**: transaction ids, in which transactions you voted.
  - **withdrawn voting record ids**: transaction ids.
  - **active voted votes amount**: the number(excluding the withdrawn) of token you vote.
  - **all voted votes amount**: the number of token you have voted.
  - **active voting records**: no records in this api.
  - **withdrawn votes records**: no records in this api.
  - **pubkey**: voter public key (byte string).

### GetElectorVoteWithRecords

```Protobuf
rpc GetElectorVoteWithRecords (google.protobuf.StringValue) returns (ElectorVote){}

message StringValue {
  string value = 1;
}

message ElectorVote {
    repeated aelf.Hash active_voting_record_ids = 1;// Not withdrawn.
    repeated aelf.Hash withdrawn_voting_record_ids = 2;
    sint64 active_voted_votes_amount = 3;
    sint64 all_voted_votes_amount = 4;
    repeated ElectionVotingRecord active_voting_records = 5;
    repeated ElectionVotingRecord withdrawn_votes_records = 6;
    bytes pubkey = 7;
}

message ElectionVotingRecord {
    aelf.Address voter = 1;
    string candidate = 2;
    sint64 amount = 3;
    sint64 term_number = 4;
    aelf.Hash vote_id = 5;
    sint64 lock_time = 7;
    google.protobuf.Timestamp unlock_timestamp = 10;
    google.protobuf.Timestamp withdraw_timestamp = 11;
    google.protobuf.Timestamp vote_timestamp = 12;
    bool is_withdrawn = 13;
    sint64 weight = 14;
    bool is_change_target = 15;
}
```

Gets the information about a voter including the votes (excluding withdrawal information).

- **StringValue**
  - **value**: the public key (hexadecimal string) of the voter.

- **Returns**
  - **active voting record ids**: transaction ids, in which transactions you vote.
  - **withdrawn voting record ids**: transaction ids.
  - **active voted votes amount**: the number(excluding the withdrawn) of token you vote.
  - **all voted votes amount**: the number of token you have voted.
  - **active voting records**: records of the vote transaction with detail information.
  - **withdrawn votes records**: no records in this api.
  - **pubkey**: voter public key (byte string).

- **ElectionVotingRecord**
  - **voter**: voter address.
  - **candidate**: public key.
  - **amount**: vote amount.
  - **term number**: snapshot number.
  - **vote id**: transaction id.
  - **lock time**: time left to unlock token.
  - **unlock timestamp**: unlock date.
  - **withdraw timestamp**: withdraw date.
  - **vote timestamp**: vote date.
  - **is withdrawn**: indicates if the vote has been withdrawn.
  - **weight**: vote weight for sharing bonus. 
  - **is change target**: whether vote others.

### GetElectorVoteWithAllRecords

```Protobuf
rpc GetElectorVoteWithAllRecords (google.protobuf.StringValue) returns (ElectorVote){}

message StringValue {
  string value = 1;
}

message ElectorVote {
    repeated aelf.Hash active_voting_record_ids = 1;// Not withdrawn.
    repeated aelf.Hash withdrawn_voting_record_ids = 2;
    sint64 active_voted_votes_amount = 3;
    sint64 all_voted_votes_amount = 4;
    repeated ElectionVotingRecord active_voting_records = 5;
    repeated ElectionVotingRecord withdrawn_votes_records = 6;
    bytes pubkey = 7;
}


message ElectionVotingRecord {
    aelf.Address voter = 1;
    string candidate = 2;
    sint64 amount = 3;
    sint64 term_number = 4;
    aelf.Hash vote_id = 5;
    sint64 lock_time = 7;
    google.protobuf.Timestamp unlock_timestamp = 10;
    google.protobuf.Timestamp withdraw_timestamp = 11;
    google.protobuf.Timestamp vote_timestamp = 12;
    bool is_withdrawn = 13;
    sint64 weight = 14;
    bool is_change_target = 15;
}
```

Gets the information about a voter including the votes and withdrawal information.

- **StringValue**
  - **value**: the public key (hexadecimal string) of voter.

- **Returns**
  - **active voting record ids**: transaction ids, in which transactions you vote.
  - **withdrawn voting record ids**: transaction ids.
  - **active voted votes amount**: the number(excluding the withdrawn) of token you vote.
  - **all voted votes amount**: the number of token you have voted.
  - **active voting records**: records of transactions that are active.
  - **withdrawn votes records**: records of transactions in which withdraw is true.
  - **pubkey**: voter public key (byte string).

- **ElectionVotingRecord**
  - **voter**: voter address.
  - **candidate**: public key.
  - **amount**: vote amount.
  - **term number**: snapshot number.
  - **vote id**: transaction id.
  - **lock time**: time left to unlock token.
  - **unlock timestamp**: unlock date.
  - **withdraw timestamp**: withdraw date.
  - **vote timestamp**: vote date.
  - **is withdrawn**: indicates if the vote has been withdrawn.
  - **weight**: vote weight for sharing bonus.
  - **is change target**: whether vote others.

### GetCandidateVote

```Protobuf
rpc GetCandidateVote (google.protobuf.StringValue) returns (CandidateVote){}

message StringValue {
  string value = 1;
}

message CandidateVote {
    repeated aelf.Hash obtained_active_voting_record_ids = 1;
    repeated aelf.Hash obtained_withdrawn_voting_record_ids = 2;
    sint64 obtained_active_voted_votes_amount = 3;
    sint64 all_obtained_voted_votes_amount = 4;
    repeated ElectionVotingRecord obtained_active_voting_records = 5;
    repeated ElectionVotingRecord obtained_withdrawn_votes_records = 6;
    bytes pubkey = 7;
}
```

Gets statistical information about vote transactions of a candidate.

- **StringValue**
  - **value**: public key of the candidate.

- **Returns**
  - **obtained active voting record ids**: vote transaction ids.
  - **obtained withdrawn voting record ids**: withdrawn transaction ids.
  - **obtained active voted votes amount**: the valid number of vote token in current.
  - **all obtained voted votes amount**: total number of vote token the candidate has got.
  - **obtained active voting records**: no records in this api.
  - **obtained withdrawn votes records**: no records in this api.

### GetCandidateVoteWithRecords

```Protobuf
rpc GetCandidateVoteWithRecords (google.protobuf.StringValue) returns (CandidateVote){}

message StringValue {
  string value = 1;
}

message CandidateVote {
    repeated aelf.Hash obtained_active_voting_record_ids = 1;
    repeated aelf.Hash obtained_withdrawn_voting_record_ids = 2;
    sint64 obtained_active_voted_votes_amount = 3;
    sint64 all_obtained_voted_votes_amount = 4;
    repeated ElectionVotingRecord obtained_active_voting_records = 5;
    repeated ElectionVotingRecord obtained_withdrawn_votes_records = 6;
    bytes pubkey = 7;
}
message ElectionVotingRecord {
    aelf.Address voter = 1;
    string candidate = 2;
    sint64 amount = 3;
    sint64 term_number = 4;
    aelf.Hash vote_id = 5;
    sint64 lock_time = 7;
    google.protobuf.Timestamp unlock_timestamp = 10;
    google.protobuf.Timestamp withdraw_timestamp = 11;
    google.protobuf.Timestamp vote_timestamp = 12;
    bool is_withdrawn = 13;
    sint64 weight = 14;
    bool is_change_target = 15;
}
```

Gets statistical information about vote transactions of a candidate with the detailed information of the transactions that are not withdrawn.

- **StringValue**
  - **value**: public key of the candidate.

- **Returns**
  - **obtained active voting record ids**: vote transaction ids.
  - **obtained withdrawn voting record ids**: withdraw transaction ids.
  - **obtained active voted votes amount**: the valid number of vote token in current.
  - **all obtained voted votes amount**: total number of vote token the candidate has got.
  - **obtained active voting records**: the records of the transaction without withdrawing.
  - **obtained withdrawn votes records**: no records in this api.

- **ElectionVotingRecord**
  - **voter** voter address.
  - **candidate** public key.
  - **amount** vote amount.
  - **term number**  snapshot number.
  - **vote id** transaction id.
  - **lock time** time left to unlock token.
  - **unlock timestamp**  unlock date.
  - **withdraw timestamp** withdraw date.
  - **vote timestamp** vote date.
  - **is withdrawn** indicates whether the vote has been withdrawn.
  - **weight**  vote weight for sharing bonus. 
  - **is change target** whether vote others.

### GetCandidateVoteWithAllRecords

```Protobuf
rpc GetCandidateVoteWithAllRecords (google.protobuf.StringValue) returns (CandidateVote){}

message StringValue {
  string value = 1;
}

message CandidateVote {
    repeated aelf.Hash obtained_active_voting_record_ids = 1;
    repeated aelf.Hash obtained_withdrawn_voting_record_ids = 2;
    sint64 obtained_active_voted_votes_amount = 3;
    sint64 all_obtained_voted_votes_amount = 4;
    repeated ElectionVotingRecord obtained_active_voting_records = 5;
    repeated ElectionVotingRecord obtained_withdrawn_votes_records = 6;
    bytes pubkey = 7;
}

message ElectionVotingRecord {
    aelf.Address voter = 1;
    string candidate = 2;
    sint64 amount = 3;
    sint64 term_number = 4;
    aelf.Hash vote_id = 5;
    sint64 lock_time = 7;
    google.protobuf.Timestamp unlock_timestamp = 10;
    google.protobuf.Timestamp withdraw_timestamp = 11;
    google.protobuf.Timestamp vote_timestamp = 12;
    bool is_withdrawn = 13;
    sint64 weight = 14;
    bool is_change_target = 15;
}
```

Gets statistical information about vote transactions of a candidate with the detailed information of all the transactions.

- **StringValue**
  - **value**: public key of the candidate.

- **Returns**
  - **obtained active voting record ids**: vote transaction ids.
  - **obtained withdrawn voting record ids**: withdrawn transaction ids.
  - **obtained active voted votes amount**: the valid number of vote token in current.
  - **all obtained voted votes amount**: total number of vote token the candidate has got.
  - **obtained active voting records**: the records of the transaction without withdrawing.
  - **obtained withdrawn votes records**: the records of the transaction withdrawing the vote token.

- **ElectionVotingRecord**
  - **voter**: voter address.
  - **candidate**: public key.
  - **amount**: vote amount.
  - **term number**: snapshot number.
  - **vote id**: transaction id.
  - **lock time**: time left to unlock token.
  - **unlock timestamp**: unlock date.
  - **withdraw timestamp**: withdraw date.
  - **vote timestamp**: vote date.
  - **is withdrawn**: indicates whether the vote has been withdrawn.
  - **weight**: vote weight for sharing bonus.
  - **is change target**: whether vote others.

### GetVotersCount

```Protobuf
rpc GetVotersCount (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Gets the total number of voters.

- **Returns**
  - **value**: number of voters.

### GetVotesAmount

```Protobuf
rpc GetVotesAmount (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Gets the total number of vote token (not counting those that have been withdrawn).

- **Returns**
  - **value**: number of vote token.

### GetCurrentMiningReward

```Protobuf
rpc GetCurrentMiningReward (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Gets the current block reward (produced block Number times reward unit).

- **Returns**
  - **value**: number of ELF that rewards miner for producing blocks.

### GetPageableCandidateInformation

```Protobuf
rpc GetPageableCandidateInformation (PageInformation) returns (GetPageableCandidateInformationOutput){}

message PageInformation {
    sint32 start = 1;
    sint32 length = 2;
}

message GetPageableCandidateInformationOutput {
    repeated CandidateDetail value = 1;
}

message CandidateDetail {
    CandidateInformation candidate_information = 1;
    sint64 obtained_votes_amount = 2;
}

message CandidateInformation {
    string pubkey = 1;
    repeated sint64 terms = 2;
    sint64 produced_blocks = 3;
    sint64 missed_time_slots = 4;
    sint64 continual_appointment_count = 5;
    aelf.Hash announcement_transaction_id = 6;
    bool is_current_candidate = 7;
}
```

Gets candidates' information according to the page's index and records length.

- **PageInformation**
  - **start**: start index.
  - **length**: number of records.

- **Returns**
  - **CandidateDetail**: candidates' detailed information.

- **CandidateDetail**
  - **candidate information**: candidate information.
  - **obtained votes amount**: obtained votes amount.

- **CandidateInformation**
  - **pubkey**: public key (hexadecimal string).
  - **terms**: indicate which terms have the candidate participated.
  - **produced blocks**: the number of blocks the candidate has produced. 
  - **missed time slots**: the time the candidate failed to produce blocks.
  - **continual appointment count**: the time the candidate continue to participate in the election.
  - **announcement transaction id**: the transaction id that the candidate announce.
  - **is current candidate**: indicate whether the candidate can be elected in the current term.

### GetMinerElectionVotingItemId

```Protobuf
rpc GetMinerElectionVotingItemId (google.protobuf.Empty) returns (aelf.Hash){}

message Hash
{
    bytes value = 1;
}
```

Gets the voting activity id.

- **Returns**
  - **value**: voting item id.

### GetDataCenterRankingList

```Protobuf
rpc GetDataCenterRankingList (google.protobuf.Empty) returns (DataCenterRankingList){}

message DataCenterRankingList {
    map<string, sint64> data_centers = 1;
    sint64 minimum_votes = 2;
}
```

Gets the data center ranking list.

- **Returns**
  - **data centers**: the top n * 5 candidates with vote amount.
  - **minimum votes**: not be used.

### GetVoteWeightProportion

```Protobuf
rpc GetVoteWeightProportion (google.protobuf.Empty) returns (VoteWeightProportion){}

message VoteWeightProportion {
    int32 time_proportion = 1;
    int32 amount_proportion = 2;
}
```

Gets VoteWeight Proportion.

note: *for VoteWeightProportion see SetVoteWeightProportion*

### GetCalculateVoteWeight

```Protobuf
rpc GetCalculateVoteWeight (VoteInformation) returns (google.protobuf.Int64Value){}

message VoteInformation{
    int64 amount = 1;
    int64 lock_time = 2;
}
```

Calculate the concrete vote weight according to your input.

- **VoteInformation**
  - **amount**: the vote amount.
  - **lock time**: the lock time your vote.

- **Returns**
  - **value**: vote weight calculated with your input and our function.
  