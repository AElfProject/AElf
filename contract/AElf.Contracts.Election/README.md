# Election Contract

## Actions

<details>

  <summary><b>InitialElectionContract</b></summary>

This method will be called once by an inline transaction right after `Election Contract` get deployed.

### Purpose

Set contract system name of `Token Contract` and `Vote Contract` in order to get their addresses in the future.

### Notes

- Contract system names can neither be same nor empty.

- Cannot initialize more than once.

</details>

<details>

  <summary><b>AnnounceElection</b></summary>

### Purpose

For a `Candidate` to annouce election.

### Notes

- Will lock a certain amount (`ElectionContractConsts.LockTokenForElection`) of ELF token of this `Candidate`.

</details>

<details>

  <summary><b>QuitElection</b></summary>

### Purpose

For a `Candidate` to quit election.

### Notes

- Will unlock a certain amount (`ElectionContractConsts.LockTokenForElection`) of ELF token of this `Candidate`.

</details>

<details>

  <summary><b>Vote</b></summary>

### Purpose

For a `Voter` to vote for his favorate `Candidate`.

### Notes

</details>

<details>

  <summary><b>Withdraw</b></summary>

### Purpose

For a `Voter` to withdraw his votes.

### Notes

</details>

<details>

  <summary><b>UpdateTermNumber</b></summary>

### Purpose

For a `Sponsor` to update term. Actually the `Sponsor` is `Consensus Contract`.

### Notes

</details>
## Views
