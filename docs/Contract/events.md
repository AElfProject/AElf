## Smart contract events

#### Event option

```json
message Transferred {
    option (aelf.is_event) = true;
    Address from = 1;
    Address to = 2;
    string symbol = 3;
    sint64 amount = 4;
}
```