# Some basic designs
- `Vote Contract` provides methods like `Register` a voting topic, `Vote` to a voting topic, and `Withdraw` votes, which is supposed to be a basic voting contract that sponsors are likely to writing a new contract to call methods of this contract.
- When `total_epoch` is `x`, if sponsor set `epoch_number` to `x + 1`, this voting topic will be terminated immediately.