# Consensus Contract

The Consensus contract is essentially used for managing block producers and synchronizing data.

## **view methods**

For reference, you can find here the available view methods.

### GetCurrentMinerList

```Protobuf
rpc GetCurrentMinerList (google.protobuf.Empty) returns (MinerList){}

message MinerList {
    repeated bytes pubkeys = 1;
}
```

Gets the list of current miners.

- **Returns**
  - **pubkeys**: miners' public keys.

### GetCurrentMinerPubkeyList

```Protobuf
rpc GetCurrentMinerPubkeyList (google.protobuf.Empty) returns (PubkeyList){}

message PubkeyList {
    repeated string pubkeys = 1;
}
```

Gets the list of current miners, each item a block producer's public key in hexadecimal format.

- **Returns**
  - **pubkeys**: miner's public key (hexadecimal string).

### GetCurrentMinerListWithRoundNumber

```Protobuf
rpc GetCurrentMinerListWithRoundNumber (google.protobuf.Empty) returns (MinerListWithRoundNumber){}

message MinerListWithRoundNumber {
    MinerList miner_list = 1;
    sint64 round_number = 2;
}

message MinerList {
    repeated bytes pubkeys = 1;
}
```

Gets the list of current miners along with the round number.

- **Returns**
  - **miner list**: miners list.
  - **round number**: current round number.

- **MinerList**
  - **pubkeys**: miners' public keys.

### GetRoundInformation

```Protobuf
rpc GetRoundInformation (aelf.SInt64Value) returns (Round){}

message SInt64Value
{
    sint64 value = 1;
}

message Round {
    sint64 round_number = 1;
    map<string, MinerInRound> real time miners information = 2;
    sint64 main chain miners round number = 3;
    sint64 blockchain age = 4;
    string extra block producer of previous round = 7;
    sint64 term number = 8;
    sint64 confirmed irreversible block height = 9;
    sint64 confirmed irreversible block round number = 10;
    bool is miner list just changed = 11;
    sint64 round id for validation = 12;
}

message MinerInRound {
    sint32 order = 1;
    bool is extra block producer = 2;
    aelf.Hash in value = 3;
    aelf.Hash out value = 4;
    aelf.Hash signature = 5;
    google.protobuf.Timestamp expected mining time = 6;
    sint64 produced blocks = 7;
    sint64 missed time slots = 8;
    string pubkey = 9;
    aelf.Hash previous in value = 12;
    sint32 supposed order of next round = 13;
    sint32 final order of next round = 14;
    repeated google.protobuf.Timestamp actual mining times = 15;
    map<string, bytes> encrypted pieces = 16;
    map<string, bytes> decrypted pieces = 17;
    sint32 produced tiny blocks = 18;
    sint64 implied irreversible block height = 19;
}
```

Gets information of the round specified as input.

- **SInt64Value**
  - **value**: round number.

- **Returns**
  - **round number**: round number.
  - **real time miners information**: public key => miner information.
  - **blockchain age**: current time minus block chain start time (if the round number is 1, the block chain age is 1), represented in seconds.
  - **extra block producer of previous round**: the public key (hexadecimal string) of the first miner, who comes from the last term, in the current term.
  - **term number**: the current term number.
  - **confirmed irreversible block height**: irreversible block height.
  - **confirmed irreversible block round number**: irreversible block round number.
  - **is miner list just changed**: is miner list different from the the miner list in the previous round.
  - **round id for validation**: round id, calculated by summing block producers' expecting time (second).

- **MinerInRound**
  - **order**: the order of miners producing block.
  - **is extra block producer**: The miner who is the first miner in the first round of each term.
  - **in value**: the previous miner's public key.
  - **out value**: the post miner's public key.
  - **signature**: self signature.
  - **expected mining time**: expected mining time.
  - **produced blocks**: produced blocks.
  - **missed time slots**: missed time slots.
  - **pubkey**: public key string.
  - **previous in value**: previous miner's public key.
  - **supposed order of next round**: evaluated order in next round.
  - **final order of next round**: the real order in the next round.
  - **actual mining times**: the real mining time.
  - **encrypted pieces**: public key (miners in the current round) =>  message encrypted by shares information and public key (represented by hexadecimal string).
  - **decrypted pieces**: the message of miners in the previous round.
  - **produced tiny blocks**: produced tiny blocks.
  - **implied irreversible block height**: miner records a irreversible block height.

### GetCurrentRoundNumber

```Protobuf
rpc GetCurrentRoundNumber (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Gets the current round number.

- **Returns**
  - **value**: number of current round.

### GetCurrentRoundInformation

Gets the current round's information.

```Protobuf
 rpc GetCurrentRoundInformation (google.protobuf.Empty) returns (Round){}
```

note: *for Round see GetRoundInformation*

### GetPreviousRoundInformation

Gets the previous round information.

```Protobuf
rpc GetPreviousRoundInformation (google.protobuf.Empty) returns (Round){}
```

note: *for Round see GetRoundInformation*

### GetCurrentTermNumber

```Protobuf
rpc GetCurrentTermNumber (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Gets the current term number.

- **Returns**
  - **value**: the current term number.

### GetCurrentWelfareReward

```Protobuf
rpc GetCurrentWelfareReward (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Gets the current welfare reward.

- **Returns**
  - **value**: the current welfare reward.

### GetPreviousMinerList

```Protobuf
rpc GetPreviousMinerList (google.protobuf.Empty) returns (MinerList){}

message MinerList {
    repeated bytes pubkeys = 1;
}
```

Gets the miners in the previous term.

- **MinerList**
  - **pubkeys**: public keys (represented by hexadecimal strings) of miners in the previous term.

### GetMinedBlocksOfPreviousTerm

```Protobuf
rpc GetMinedBlocksOfPreviousTerm (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Gets the number of mined blocks during the previous term.

- **Returns**
  - **value**: the number of mined blocks.

### GetNextMinerPubkey

```Protobuf
rpc GetNextMinerPubkey (google.protobuf.Empty) returns (google.protobuf.StringValue){}

message StringValue {
  string value = 1;
}
```

Gets the miner who will produce the block next, which means the miner is the first one whose expected mining time is greater than the current time. If this miner can not be found, the first miner who is extra block producer will be selected.

- **Returns**
  - **value**: the miner's public key.

### GetCurrentMinerPubkey

```Protobuf
rpc GetCurrentMinerPubkey (google.protobuf.Empty) returns (google.protobuf.StringValue){}

message StringValue {
  string value = 1;
}
```

Gets the current miner.

- **Returns**:
  - **value**: miner's public key.

### IsCurrentMiner

```Protobuf
rpc IsCurrentMiner (aelf.Address) returns (google.protobuf.BoolValue){
}

message Address
{
    bytes value = 1;
}
```

Query whether the miner is the current miner.

- **Address**
  - **value**: miner's address.

- **Returns**
  - **value**: indicates if the input miner is the current miner.

### GetNextElectCountDown

```Protobuf
rpc GetNextElectCountDown (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Count down to the next election.

- **Returns**
  - **value**: total seconds to next election.
