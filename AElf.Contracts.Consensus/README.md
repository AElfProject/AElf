# For candidates

## Announce election
Send transaction `AnnouceElection` to `TokenContract`;

## Quit election
Send transaction `QuitElection` to `ConsensusContract`

# For voters

## Get tickets
Send transaction `GetTickets` to `TokenContract`

## Vote
Send transaction `Vote` to `ConsensusContract`

## Regret
Send transaction `Regret` to `ConsensusContract`

## Withdraw (give up tickets to get ELFs back)
Send transaction `Withdraw` to `ConsensusContract`

- Can only withdraw remaining tickets, which means if anyone want to withdraw all tickets, first regret his votings.
