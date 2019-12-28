# Election Contract

The Election contract is essentially used for voting for Block Producers.

## **an Election for choosing Block Producers**:

take snapshot used by AEDPos contract
```Protobuf
rpc TakeSnapshot (TakeElectionSnapshotInput) returns (google.protobuf.Empty) {}

message TakeElectionSnapshotInput {
    sint64 term_number = 1;
    sint64 mined_blocks = 2;
    sint64 round_number = 3;
}
```

- **term number**   candidate id
- **mined blocks**   amount token to vote
- **round number**  before which, your vote works.





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
Q: public key, address
```Protobuf
rpc ChangeVotingOption (ChangeVotingOptionInput) returns google.protobuf.Empty){}

message ChangeVotingOptionInput {
    aelf.Hash vote_id = 1;
    string candidate_pubkey = 2;
}
```

- **voting vote id** transaction id.
- **candidate pubkey**  new candidate public key



After the end time mentioned in the api about vote, your deposit token will be unlocked, and you can withdraw them.

```Protobuf
rpc Withdraw (aelf.Hash) returns (google.protobuf.Empty) {}

message Hash
{
    bytes value = 1;
}
```

- **value**  transaction id



## **an Election for choosing Block Producers (Views)**:
get all candidates

Q: return bytes list?

```Protobuf
rpc GetCandidates (google.protobuf.Empty) returns (PubkeyList) {}

message PubkeyList {
    repeated bytes value = 1;
}
```

- **value** public key array of candidates


get all candidates who has got vote

Q: return bytes list not string?

```Protobuf
rpc GetVotedCandidates (google.protobuf.Empty) returns (PubkeyList) {}

message PubkeyList {
    repeated bytes value = 1;
}
```

- **value** public key array of candidates





Get the Candidate information. If the candidate does not exist, it will return a candidate without any information to you.

Q : UpdateCandidateInformation  authorization needed ??

```Protobuf
rpc GetCandidateInformation (google.protobuf.StringValue) returns (CandidateInformation) {}

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

StringValue
- **value** public key (hexadecimal string) of the candidate

CandidateInformation
- **pubkey** public key (hexadecimal string)
- **terms** indicate which terms have the candidate participated
- **produced blocks** the number of blocks the candidate has produced 
- **missed time slots** the time the candidate failed to produce blocks
- **continual appointment count** the time the candidate continue to participate in the election
- **announcement transaction id** the transaction id that the candidate announce
- **is current candidate** indicate whether the candidate can be elected in the current term.



GetVictories  to do

```Protobuf
rpc GetVictories (google.protobuf.Empty) returns (PubkeyList) {}

message PubkeyList {
    repeated bytes value = 1;
}
```

- **value** the array of public key who has been elected as block producer




Get term information by term id.

```Protobuf
rpc GetTermSnapshot (GetTermSnapshotInput) returns (TermSnapshot) {}

message GetTermSnapshotInput {
    sint64 term_number = 1;
}

message TermSnapshot {
    sint64 end_round_number = 1;
    sint64 mined_blocks = 2;
    map<string, sint64> election_result = 3;
}
```
GetTermSnapshotInput
- **term number ** term id

TermSnapshot
- **end round number** the last term id be saved
- **mined blocks** number of blocks produced in previous term
- **election result** candidate => votes

Get miners count

```Protobuf
rpc GetMinersCount (google.protobuf.Empty) returns (aelf.SInt32Value) {}

message SInt32Value
{
    sint32 value = 1;
}
```

- **value** the total number of miners (block producer)




Get election result by term id

```Protobuf
rpc GetElectionResult (GetElectionResultInput) returns (ElectionResult) {}

message GetElectionResultInput {
    sint64 term_number = 1;
}

message ElectionResult {
    sint64 term_number = 1;
    map<string, sint64> results = 2;
    bool is_active = 3;
}
```

GetElectionResultInput
- **term number** term id

ElectionResult
- **term number** term id
- **results** candidate => votes
- **is active** indicates that if the term number you input is the current term





Get voter info

```Protobuf
rpc GetElectorVote (google.protobuf.StringValue) returns (ElectorVote) {}

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
StringValue
- **value** the public key(hexadecimal string) of voter

ElectorVote
- **active voting record ids** transaction ids, in which transactions you vote
- **withdrawn voting record ids** transaction ids
- **active voted votes amount**  the number of token you vote and is valid(in case of withdraw)
- **all voted votes amount**  the number of token you have voted
- **active voting records** no record in this api
- **withdrawn votes records** no record in this api
- **pubkey** voter public key (byte string)



Get voter information with active transactions' concrete information

Q: split into 3 api,   address,  public key byte string, public key hexadecimal string

```Protobuf
rpc GetElectorVoteWithRecords (google.protobuf.StringValue) returns (ElectorVote) {}

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

StringValue
- **value** the public key(hexadecimal string) of voter

ElectorVote
- **active voting record ids** transaction ids, in which transactions you vote
- **withdrawn voting record ids** transaction ids
- **active voted votes amount**  the number of token you vote and is valid(in case of withdraw)
- **all voted votes amount**  the number of token you have voted
- **active voting records** records of the vote transaction with detail information
- **withdrawn votes records** no record in this api
- **pubkey** voter public key (byte string)


ElectionVotingRecord
- **voter** voter address
- **candidate** public key 
- **amount** vote amount
- **term number**  snapshot number
- **vote id** transaction id
- **lock time** time left to unlock token
- **unlock timestamp**  unlock date
- **withdraw timestamp** withdraw date
- **vote timestamp** vote date
- **is withdrawn** has withdrawn at least one time
- **weight**  vote weight for sharing bonus 
- **is change target** whether vote others




Get voter information with all transactions' concrete information

```Protobuf
rpc GetElectorVoteWithAllRecords (google.protobuf.StringValue) returns (ElectorVote) {}

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
StringValue
- **value** the public key(hexadecimal string) of voter

ElectorVote
- **active voting record ids** transaction ids, in which transactions you vote
- **withdrawn voting record ids** transaction ids
- **active voted votes amount**  the number of token you vote and is valid(in case of withdraw)
- **all voted votes amount**  the number of token you have voted
- **active voting records** records of transactions that are active
- **withdrawn votes records** records of transactions in which withdraw is true 
- **pubkey** voter public key (byte string)

ElectionVotingRecord
- **voter** voter address
- **candidate** public key 
- **amount** vote amount
- **term number**  snapshot number
- **vote id** transaction id
- **lock time** time left to unlock token
- **unlock timestamp**  unlock date
- **withdraw timestamp** withdraw date
- **vote timestamp** vote date
- **is withdrawn** has withdrawn at least one time
- **weight**  vote weight for sharing bonus 
- **is change target** whether vote others



Get a candidate vote statistical information

```Protobuf
rpc GetCandidateVote (google.protobuf.StringValue) returns (CandidateVote) {}

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
StringValue
- **value** public key of the candidate.

CandidateVote
- **obtained active voting record ids** vote transaction ids
- **obtained withdrawn voting record ids** withdraw transaction ids
- **obtained active voted votes amount** the valid number of vote token in current
- **all obtained voted votes amount** total number of vote token the candidate has got
- **obtained active voting records** no records in this api
- **obtained withdrawn votes records** no records in this api



Get a candidate vote statistical information with obtained active voting records
Q: the property is_withdraw can be deleted
```Protobuf
rpc GetCandidateVoteWithRecords (google.protobuf.StringValue) returns (CandidateVote) {}

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

StringValue
- **value** public key of the candidate.

CandidateVote
- **obtained active voting record ids** vote transaction ids
- **obtained withdrawn voting record ids** withdraw transaction ids
- **obtained active voted votes amount** the valid number of vote token in current
- **all obtained voted votes amount** total number of vote token the candidate has got
- **obtained active voting records** the records of the transaction without withdrawing
- **obtained withdrawn votes records** no records in this api

ElectionVotingRecord
- **voter** voter address
- **candidate** public key 
- **amount** vote amount
- **term number**  snapshot number
- **vote id** transaction id
- **lock time** time left to unlock token
- **unlock timestamp**  unlock date
- **withdraw timestamp** withdraw date
- **vote timestamp** vote date
- **is withdrawn** has withdrawn at least one time
- **weight**  vote weight for sharing bonus 
- **is change target** whether vote others





Besides obtained active voting records, it includes withdraw records

```Protobuf
  rpc GetCandidateVoteWithAllRecords (google.protobuf.StringValue) returns (CandidateVote) {}

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

StringValue
- **value** public key of the candidate.

CandidateVote
- **obtained active voting record ids** vote transaction ids
- **obtained withdrawn voting record ids** withdraw transaction ids
- **obtained active voted votes amount** the valid number of vote token in current
- **all obtained voted votes amount** total number of vote token the candidate has got
- **obtained active voting records** the records of the transaction without withdrawing
- **obtained withdrawn votes records** the records of the transaction withdrawing the vote token

ElectionVotingRecord
- **voter** voter address
- **candidate** public key 
- **amount** vote amount
- **term number**  snapshot number
- **vote id** transaction id
- **lock time** time left to unlock token
- **unlock timestamp**  unlock date
- **withdraw timestamp** withdraw date
- **vote timestamp** vote date
- **is withdrawn** has withdrawn at least one time
- **weight**  vote weight for sharing bonus 
- **is change target** whether vote others





Get total number of voters

```Protobuf
 rpc GetVotersCount (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```

- **value** number of voters



Get total number of vote token (excluding that has been withdrawn)

```Protobuf
rpc GetVotesAmount (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```

- **value** number of vote token



Get current block reward (produced block No. multiply unit reward)

```Protobuf
 rpc GetCurrentMiningReward (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```

- **value** number of ELF that rewards miner for producing blocks



GetWelfareRewardAmountSample  to do    
Q: not used

```Protobuf
rpc GetWelfareRewardAmountSample (GetWelfareRewardAmountSampleInput) returns (GetWelfareRewardAmountSampleOutput) {}

message GetWelfareRewardAmountSampleInput {
    repeated sint64 value = 1;
}

message GetWelfareRewardAmountSampleOutput {
    repeated sint64 value = 1;
}
```
- **value** hash id of all candidate.



Get candidates' information according to the index and length

```Protobuf
rpc GetPageableCandidateInformation (PageInformation) returns (GetPageableCandidateInformationOutput) {}

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
PageInformation
- **start** start index
- **length** number of records

GetPageableCandidateInformationOutput
- **CandidateDetail**  candidate detail information

CandidateDetail
- **candidate information** candidate information
- **obtained votes amount** obtained votes amount

CandidateInformation
- **pubkey** public key (hexadecimal string)
- **terms** indicate which terms have the candidate participated
- **produced blocks** the number of blocks the candidate has produced 
- **missed time slots** the time the candidate failed to produce blocks
- **continual appointment count** the time the candidate continue to participate in the election
- **announcement transaction id** the transaction id that the candidate announce
- **is current candidate** indicate whether the candidate can be elected in the current term.



Get miner election voting itemId  //to do how to describe it

```Protobuf
rpc GetMinerElectionVotingItemId (google.protobuf.Empty) returns (aelf.Hash) {}

message Hash
{
    bytes value = 1;
}
```

- **value** voting item id



GetDataCenterRankingList  to do
Q: minimum votes is not used,  and whether rank list is sorted?
```Protobuf
rpc GetDataCenterRankingList (google.protobuf.Empty) returns (DataCenterRankingList) {}

message DataCenterRankingList {
    map<string, sint64> data_centers = 1;
    sint64 minimum_votes = 2;
}
```

- **data centers** the top n * 5 candidates with concrete vote amount
- **minimum votes** not used

