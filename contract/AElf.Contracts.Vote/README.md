# Vote Contract

## Actions 
<details>

  <summary><b>InitialVoteContract</b></summary>

This method will be called once by an inline transaction right after `Vote Contract` get deployed.

### Purpose

Set contract system name of `Token Contract` in order to get its addresses in the future.

### Notes

- Sender must be the owner of `Vote Contract`, which should be the address of `Basic Contract Zero`.

- Contract system names can neither be same nor empty.

- Cannot initialize more than once.

</details>

<details>

  <summary><b>Register</b></summary>

### Purpose

For a `Sponsor` to register / create a voting event.

### Notes

- Transction sender will be the `Sponsor`.

- The values of `Sponsor` fields can identify a `VotingEvent`.

- A `VotingEvent` with specific `EpochNumber` called `VotingGoing` in this contract, which isn't really exists.

- Thus we can use `GetHash()` of `VotingResult` to get the hash of a `VotingGoing`.

- If `Delegated` is true, it means the sender address of `Vote` transaction must be the address of `Sponsor`.

- If `StartTimestamp` of input value is smaller than current block time, will use current block time as `StartTimestamp`.

- Cannot create a voting event with maximum active time but only 1 epoch. This means voter can never with their votes. Also, voters cannot vote to a voting event with maximum active time in its last epoch.

- Anyway, voters can withdraw their votes after a certain days according to the value of `VoteContractConsts.MaxActiveDays`.

</details>

<details>

  <summary><b>Vote</b></summary>
  
### Purpose

For a `Voter` to vote for a voting going (a epoch of a voting event).

### Notes

- Basically, a voting behaviour is to update related `VotingResult` and `VotingHistories`, also add a new `VotingRecord`.

- `VotingHistories` contains vote histories of all `VotingEvent`s - more precisely - `VotingGoing`s of a voter.

- `VotingHistory` just for one `VotingGoing` (of a voter).

- The values of `Sponsor` and `EpochNumber` fields can identify a `VotingGoing` or a `VotingResult`.

- We can get a certain `VotingRecord` by providing transaction id of `Vote` transaction, which actually called `VoteId`.

- This method will only lock token if voting event isn't delegated. Delegated voting event should lock in higher level contract, like `Election Contract`.


</details>

<details>

  <summary><b>Withdraw</b></summary>

### Purpose

For a `Voter` to withdraw his previous votes.

### Notes

- Will update related `VotingResult` and `VotingRecord`.

- Unlock token logic is same as `Vote` method, delegated voting event should unlock token on 

- Cannot withdraw votes of on-going voting events, it means `EpochNumber` of `VotingRecord` must be less than `CurrentEpoch` of `VotingEvent`.

- Extra limitation of voters withdrawing their votes should be coded in higher level contract. Like in `Election Contract`, voters need to keep locking their tokens at least for several epoches (terms).

</details>

<details>

  <summary><b>UpdateEpochNumber</b></summary>

### Purpose

For the `Sponsor` to update epoch number.

### Notes

- Can only increase the epoch number 1 each time.

- Will update previous on-going voting event and initialize new on-going event.

- After updating, votes of previous epoch is possible withdrawable for voters.

- When `TotalEpoch` of `VotingEvent` is `x`, if the `Sponsor` set `EpochNumber` to `x + 1`, the whole voting event will be regarded as terminated immediately.

</details>

<details>

  <summary><b>AddOption</b></summary>

### Purpose

For the `Sponsor` to add an option of a certain `VotingEvent`.

### Notes

</details>

<details>

  <summary><b>RemoveOption</b></summary>

### Purpose

For the `Sponsor` to remove an option of a certain `VotingEvent`.

### Notes

</details>

## Views

<details>

  <summary><b>GetVotingResult</b></summary>

</details>

<details>

  <summary><b>GetVotingRecord</b></summary>

</details>

<details>

  <summary><b>GetVotingHistories</b></summary>

</details>

<details>

  <summary><b>GetVotingHistory</b></summary>

</details>