# Mining Node Management

## Related Contracts

- AElf Consensus Contract

- Election Contract

- Miners Count Provider Contract

## Initial Miners

Basically initial miners list are hard coded to `Genesis Block`.

## Voting Miners

Initial miners will be replaced as soon as we have enough voted miners.

Miners will be changed every time our Main Chain turn to next term, like every 7 days.

Miners count will be changed every year.

### Register as a candidate

Send a transaction of `AnnounceElection` to `Election Contract`, this will lock 100,000 ELF tokens of sender.

### Vote to candidates

Voters can send a transaction of `Vote` to `Election Contract` voting for any registered candidate, this will lock a certain amount a ELF tokens of this voter, and transfer `VOTE` token to him from `Election Contract`.
