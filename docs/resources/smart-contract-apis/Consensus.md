# Consensus Contract
The Consensus contract is essentially used for picking block producers and synchronizing data.

## view methods

For reference, you can find here the available view methods.

### GetCurrentMinerList

Get current miner list represented by binary.

```Protobuf
rpc GetCurrentMinerList (google.protobuf.Empty) returns (MinerList) {}

message MinerList {
    repeated bytes pubkeys = 1;
}
```

**MinerList**:
- **pubkeys**: miners' public key.

### GetCurrentMinerPubkeyList

Get current miner list represented by hexadecimal string.

```Protobuf
 rpc GetCurrentMinerPubkeyList (google.protobuf.Empty) returns (PubkeyList) {}

message PubkeyList {
    repeated string pubkeys = 1;
}
```

**PubkeyList**:
- **pubkeys**: miners' public key（hexadecimal string.

### GetCurrentMinerListWithRoundNumber

Get current miner list represented by binary and the current round number.

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

**MinerListWithRoundNumber**:
- **miner list**: miner list.
- **round number**: current round number.

**MinerList**:
- **pubkeys**: miners' public key.

### GetCurrentMinerListWithRoundNumber

Get round information.

```Protobuf
rpc GetRoundInformation (aelf.SInt64Value) returns (Round) {}
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

**SInt64Value**:
- **value**: round number.

**Round**:
- **round number**: round number.
- **real time miners information**: public key => miner information.
- **main chain miners round number**: is not used.
- **blockchain age**: current time - block chain start time stamp.
- **extra block producer of previous round**: the public key(hexadecimal string) of the first miner in the current term.
- **term number**: term number.
- **confirmed irreversible block height**: irreversible block height.
- **confirmed irreversible block round number**: irreversible block round number.
- **is miner list just changed**: is miner list different from the the miner list in the previous term.
- **round id for validation**: round id, bpInfo.ExpectedMiningTime.Seconds.sum.

**MinerInRound**:
- **order**: mining order.
- **is extra block producer**: The miner who is the first miner in the first round of each term will be the extra block producer.
- **in value**: like linked node, the previous miner's public key
- **out value**: the post miner's public key
- **signature*: self signature
- **expected mining time**: expected mining time 
- **produced blocks**: produced blocks
- **missed time slots**: missed time slots
- **pubkey**: public key string
- **previous in value**: the public key of the miner before previous miner
- **supposed order of next round**: evaluated order in next round
- **final order of next round**: the true order in the next round
- **actual mining times**: mining time.
- **encrypted pieces**: public key (miners in previous round) =>  message encrypted by shares information and public string represented by hexadecimal string.
- **decrypted pieces**: decrypt the encrypted pieces of miners in the previous round.
- **produced tiny blocks**: produced tiny blocks.
- **implied irreversible block height**: suggest the current context's height is the irreversible block height.

### GetCurrentRoundNumber

Get current round number.

```Protobuf
rpc GetCurrentRoundNumber (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```

**SInt64Value**:
- **value**: number of current round.

### GetCurrentRoundInformation

Get current round information.

```Protobuf
 rpc GetCurrentRoundInformation (google.protobuf.Empty) returns (Round) {}

```

**Round**:
- **round number**: round number.
- **real time miners information**: public key => miner information.
- **main chain miners round number**: is not used.
- **blockchain age**: current time - block chain start time stamp.
- **extra block producer of previous round**: the public key(hexadecimal string) of the first miner in the current term.
- **term number**: term number.
- **confirmed irreversible block height**: irreversible block height.
- **confirmed irreversible block round number**: irreversible block round number.
- **is miner list just changed**: is miner list different from the the miner list in the previous term.
- **round id for validation**: round id, bpInfo.ExpectedMiningTime.Seconds.sum.

**MinerInRound**:
- **order**: mining order.
- **is extra block producer**: The miner who is the first miner in the first round of each term will be the extra block producer.
- **in value**: like linked node, the previous miner's public key
- **out value**: the post miner's public key
- **signature*: self signature
- **expected mining time**: expected mining time 
- **produced blocks**: produced blocks
- **missed time slots**: missed time slots
- **pubkey**: public key string
- **previous in value**: the public key of the miner before previous miner
- **supposed order of next round**: evaluated order in next round
- **final order of next round**: the true order in the next round
- **actual mining times**: mining time.
- **encrypted pieces**: public key (miners in previous round) =>  message encrypted by shares information and public string represented by hexadecimal string.
- **decrypted pieces**: decrypt the encrypted pieces of miners in the previous round.
- **produced tiny blocks**: produced tiny blocks.
- **implied irreversible block height**: suggest the current context's height is the irreversible block height.

### GetPreviousRoundInformation

Get previous round information.

```Protobuf
rpc GetPreviousRoundInformation (google.protobuf.Empty) returns (Round) {}

```

**Round**:
- **round number**: round number.
- **real time miners information**: public key => miner information.
- **main chain miners round number**: is not used.
- **blockchain age**: current time - block chain start time stamp.
- **extra block producer of previous round**: the public key(hexadecimal string) of the first miner in the current term.
- **term number**: term number.
- **confirmed irreversible block height**: irreversible block height.
- **confirmed irreversible block round number**: irreversible block round number.
- **is miner list just changed**: is miner list different from the the miner list in the previous term.
- **round id for validation**: round id, bpInfo.ExpectedMiningTime.Seconds.sum.

**MinerInRound**:
- **order**: mining order.
- **is extra block producer**: The miner who is the first miner in the first round of each term will be the extra block producer.
- **in value**: like linked node, the previous miner's public key
- **out value**: the post miner's public key
- **signature*: self signature
- **expected mining time**: expected mining time 
- **produced blocks**: produced blocks
- **missed time slots**: missed time slots
- **pubkey**: public key string
- **previous in value**: the public key of the miner before previous miner
- **supposed order of next round**: evaluated order in next round
- **final order of next round**: the true order in the next round
- **actual mining times**: mining time.
- **encrypted pieces**: public key (miners in previous round) =>  message encrypted by shares information and public string represented by hexadecimal string.
- **decrypted pieces**: decrypt the encrypted pieces of miners in the previous round.
- **produced tiny blocks**: produced tiny blocks.
- **implied irreversible block height**: suggest the current context's height is the irreversible block height.

### GetCurrentTermNumber

Get current term number.

```Protobuf
rpc GetCurrentTermNumber (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```

**SInt64Value**:
- **value**: current term number.

### GetCurrentWelfareReward

Get current welfare reward.

```Protobuf
rpc GetCurrentWelfareReward (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```

**SInt64Value**:
- **value**: current welfare reward.

### GetPreviousMinerList

Get previous minerList.

```Protobuf
rpc GetPreviousMinerList (google.protobuf.Empty) returns (MinerList) {}

message MinerList {
    repeated bytes pubkeys = 1;
}
```

**MinerList**:
- **pubkeys**: public key represented by hexadecimal string of miners in the previous term.

### GetMinedBlocksOfPreviousTerm

Get mined blocks during the previous term.

```Protobuf
rpc GetMinedBlocksOfPreviousTerm (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}

```

**SInt64Value**:
- **value**: the number of mined blocks of previous term.

### GetNextMinerPubkey

Get the miner who will produce the first block in the next term.The first miner whose expected mining time is greater than current bock time. If this miner can not be found, the first miner who is extra block producer will be selected.

```Protobuf
rpc GetNextMinerPubkey (google.protobuf.Empty) returns (google.protobuf.StringValue) {}

message StringValue {
  string value = 1;
}
```
**StringValue**:
- **value**: miner's public key.

### GetCurrentMinerPubkey

Get the miner who producing the first block in the current term.

```Protobuf
rpc GetCurrentMinerPubkey (google.protobuf.Empty) returns (google.protobuf.StringValue) {}

message StringValue {
  string value = 1;
}
```

**StringValue**:
- **value**: miner's public key.

### IsCurrentMiner

Judge whether the miner is in the miner list in the current term.

```Protobuf
rpc IsCurrentMiner (aelf.Address) returns (google.protobuf.BoolValue) {}

message Address
{
    bytes value = 1;
}
```

**Address**:
- **value**: miner's address.

**BoolValue**:
- **value**: indicates if the input miner is the current miner.

### GetNextElectCountDown

Count down to the next election.

```Protobuf
rpc GetNextElectCountDown (google.protobuf.Empty) returns (aelf.SInt64Value) {}

message SInt64Value
{
    sint64 value = 1;
}
```

**SInt64Value**:
- **value**: total seconds to next election.
