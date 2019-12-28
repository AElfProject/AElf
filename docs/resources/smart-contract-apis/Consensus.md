# Consensus Contract
The Consensus contract is essentially used for picking block producers and synchronizing data.

## ** description**:

## view methods

For reference, you can find here the available view methods.


Get current miner list represented by binary.
```Protobuf
rpc GetCurrentMinerList (google.protobuf.Empty) returns (MinerList) {}

message MinerList {
    repeated bytes pubkeys = 1;
}
```
MinerList
- **pubkeys** miners' public key



Get current miner list represented by hexadecimal string.
```Protobuf
 rpc GetCurrentMinerPubkeyList (google.protobuf.Empty) returns (PubkeyList) {}

message PubkeyList {
    repeated string pubkeys = 1;
}
```

PubkeyList
- **pubkeys**miners' public key（hexadecimal string）




Get current miner list represented by binary with round number
```Protobuf
rpc GetCurrentMinerListWithRoundNumber (google.protobuf.Empty) returns (MinerListWithRoundNumber) {}

message MinerListWithRoundNumber {
    MinerList miner_list = 1;
    sint64 round_number = 2;
}

message MinerList {
    repeated bytes pubkeys = 1;
}
```
MinerListWithRoundNumber
- **miner list**  miner list
- **round number** current round number

MinerList
- **pubkeys** miners' public key


Get round information
```Protobuf
rpc GetRoundInformation (aelf.SInt64Value) returns (Round) {}
Q: main_chain_miners_round_number not used?
message SInt64Value
{
    sint64 value = 1;
}

message Round {
    sint64 round_number = 1;
    map<string, MinerInRound> real_time_miners_information = 2;
    sint64 main_chain_miners_round_number = 3;
    sint64 blockchain_age = 4;
    string extra_block_producer_of_previous_round = 7;
    sint64 term_number = 8;
    sint64 confirmed_irreversible_block_height = 9;
    sint64 confirmed_irreversible_block_round_number = 10;
    bool is_miner_list_just_changed = 11;
    sint64 round_id_for_validation = 12;
}

message MinerInRound {
    sint32 order = 1;
    bool is_extra_block_producer = 2;
    aelf.Hash in_value = 3;
    aelf.Hash out_value = 4;
    aelf.Hash signature = 5;
    google.protobuf.Timestamp expected_mining_time = 6;
    sint64 produced_blocks = 7;
    sint64 missed_time_slots = 8;
    string pubkey = 9;
    aelf.Hash previous_in_value = 12;
    sint32 supposed_order_of_next_round = 13;
    sint32 final_order_of_next_round = 14;
    repeated google.protobuf.Timestamp actual_mining_times = 15;
    map<string, bytes> encrypted_pieces = 16;
    map<string, bytes> decrypted_pieces = 17;
    sint32 produced_tiny_blocks = 18;
    sint64 implied_irreversible_block_height = 19;
}
```
SInt64Value
- **value** round number


Round
- **round_number**round number
- **real_time_miners_information** public key => miner information
- **main_chain_miners_round_number** to do
- **blockchain_age**current time - block chain start time stamp
- **extra_block_producer_of_previous_round** the public key(hexadecimal string) of the first miner in the current term
- **term_number**term number
- **confirmed_irreversible_block_height** irreversible block height
- **confirmed_irreversible_block_round_number**irreversible block round number
- **is_miner_list_just_changed** is miner list different from the the miner list of pre term
- **round_id_for_validation** round id,  bpInfo.ExpectedMiningTime.Seconds.sum



MinerInRound
- **order** mining order

- **is_extra_block_producer** The first miner will be the extra block producer of first round of each term.

- **in_value**  like linked node, the pre miner's public key
- **out_value** the post miner's public key
- **signature** self signature
- **expected_mining_time** expected mining time 
- **produced_blocks** produced blocks
- **missed_time_slots** missed time slots
- **pubkey** public key string
- **previous_in_value** the public key of the miner before pre miner
- **supposed_order_of_next_round** evaluated order in next round
- **final_order_of_next_round** e true order in the next round
- **actual_mining_times** mining time
- **encrypted_pieces** public key (miners in pre round) =>  message encrypted by shares information and public string represented by hexadecimal string
- **decrypted_pieces** decrypt the encrypted pieces of miners in the pre round
- **produced_tiny_blocks** produced tiny blocks
- **implied_irreversible_block_height** suggest the current context's height is the irreversible block height




Get current round number
```Protobuf
rpc GetCurrentRoundNumber (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```
SInt64Value
- **value**  number of current round



Get current round information
```Protobuf
 rpc GetCurrentRoundInformation (google.protobuf.Empty) returns (Round) {}

```
Round
describe as above




Get previous round information.
```Protobuf
rpc GetPreviousRoundInformation (google.protobuf.Empty) returns (Round) {}

```
Round
describe as above




Get current term number.
```Protobuf
rpc GetCurrentTermNumber (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```
SInt64Value
- **value**  current term number




Get current welfare reward
```Protobuf
rpc GetCurrentWelfareReward (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```

SInt64Value
- **value**  current welfare reward



Get previous minerList
```Protobuf
rpc GetPreviousMinerList (google.protobuf.Empty) returns (MinerList) {}

message MinerList {
    repeated bytes pubkeys = 1;
}
```

MinerList
- **pubkeys**  public key represented by hexadecimal string of miners in the previous term.




Get mined blocks of previous term
```Protobuf
rpc GetMinedBlocksOfPreviousTerm (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}

```
SInt64Value
- **value**  the number of mined blocks of previous term




Get the miner who will produce the first block in the next term.
```Protobuf
rpc GetNextMinerPubkey (google.protobuf.Empty) returns (google.protobuf.StringValue) {}

message StringValue {
  string value = 1;
}
```
StringValue
- **value**  The first miner whose expected mining time is greater than current bock time. If this miner can not be found, the first miner who is extra block producer will be selected.



Get the miner who producing the first block in the current term.
```Protobuf
rpc GetCurrentMinerPubkey (google.protobuf.Empty) returns (google.protobuf.StringValue) {}

message StringValue {
  string value = 1;
}
```
StringValue
- **value**  miner



judge whether the miner is in the miner list in the current term.
```Protobuf
rpc IsCurrentMiner (aelf.Address) returns (google.protobuf.BoolValue) {}

message Address
{
    bytes value = 1;
}
```
Address
- **value**  address


Count down the next election.
```Protobuf
rpc GetNextElectCountDown (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```
SInt64Value
- **value**  seconds
