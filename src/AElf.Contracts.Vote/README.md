# Some basic designs
- `Vote Contract` provides methods like `Register` a voting topic, `Vote` to a voting topic, and `Withdraw` votes, which is supposed to be a basic voting contract that sponsors are likely to writing a new contract to call methods of this contract.
- When `total_epoch` is `x`, if sponsor set `epoch_number` to `x + 1`, this voting topic will be terminated immediately.

# Vote Contract

## Actions 
<details>

  <summary><b>InitialVoteContract</b></summary>

This method will be called once by a inline transaction right after `Vote Contract` get deployed.

### Purpose

Set contract system name of `Token Contract` and `Consensus Contract` in order to get their addresses in the future.

### Notes

- Contract system names can neither be same nor empty.

- Cannot initialize more than once.

</details>

<details>

  <summary><b>Register</b></summary>

### Purpose

For a `Sponsor` to register / create a voting event.

### Notes

- Transction sender will be the `Sponsor`.

- The values of `Topic` and `Sponsor` filed can identify a voting event.

- If `StartTimestamp` of input value is smaller than current block time, will use current block time as `StartTimestamp`.

- Cannot create a voting event with maximum active time but only 1 epoch. This means voter can never with their votes. Also, voters cannot vote to a voting event with maximum active time in its last epoch.

- Anyway, voters can withdraw their votes after a certain days according to the value of `VoteContractConsts.MaxActiveDays`.


</details>

<details>

  <summary><b>Vote</b></summary>

</details>

<details>

  <summary><b>Withdraw</b></summary>

</details>

<details>

  <summary><b>UpdateEpochNumber</b></summary>

</details>

<details>

  <summary><b>AddOption</b></summary>

</details>

<details>

  <summary><b>RemoveOption</b></summary>

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