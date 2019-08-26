# Profit Contract

## Usage

<details>

  <summary><b>How to create a profit item</b></summary>

Call `CreateScheme` and provide the token symbol you gonna use in this item. Also you can set the `ExpiredPeriodNumber`.

</details>

## Actions 
<details>

  <summary><b>InitializeProfitContract</b></summary>

This method will be called once by an inline transaction right after `Profit Contract` get deployed.

### Purpose

Set contract system name of `Token Contract` in order to get its addresses in the future.

### Notes

- Sender must be the owner of `Profit Contract`, which should be the address of `Basic Contract Zero`.

- Contract system names can neither be same nor empty.

- Cannot initialize more than once.

</details>

<details>

  <summary><b>CreateScheme</b></summary>

### Purpose

For anyone to create a `ProfitItem`.

### Notes

- A `ProfitItem` will be identified by a Hash value which called `SchemeId`. This Hash value is calculated from `TransactionId` of `CreateScheme` transction and the address of `Profit Contract`.

- To create a `ProfitItem`, the creator need to provide its binded token symbol. Like for item `Treasury` created by `AElf Consensus Contract`, the binded token symbol is `ELF`.

- `ExpiredPeriodNumber` is optional.

- `CurrentPeriod` will start from 1, and increase every time when the creator call `ReleaseProfits` in the future.

</details>

<details>

  <summary><b>RemoveSubScheme</b></summary>

### Purpose

For a profit item `Creator` to register a `SubProfitItem` to one `ProfitItem` he created before.

### Notes

- Sender must be the `Creator` of the `ProfitItem` to register to.

- To register a `ProfitItem` as `SubProfitItem`, sender should provide `SchemeId`s of two `ProfitItem`s and the `Shares` of `SubProfitItem`.

- This method will actually call `AddBeneficiary` to add the `Shares` of `SubProfitItem`.

- Also, the `SchemeId` of `SubProfitItem` will be recorded to `ProfitItem`.

</details>

<details>

  <summary><b>AddBeneficiary</b></summary>

### Purpose

For a profit item `Creator` to add an Address to receive profits of one `ProfitItem` he created before.

### Notes

- `TotalShares` of this `ProfitItem` will be increased.

- Will add a `ProfitDetail` to record this addition for Beneficiary address to profit from this `ProfitItem` in the future.

- Will remove expired `ProfitDetail`s.

</details>

<details>

  <summary><b>SubWeight</b></summary>

### Purpose

For a profit item `Creator` to remove an Address to receive profits of one `ProfitItem` he created before.

### Notes

</details>

<details>

  <summary><b>DistributeProfits</b></summary>

### Purpose

For a profit item `Creator` to release an amount of profits to a virtual address calculated by `ProfitItem` and current period number.

If this `ProfitItem` has `SubProfitItem`s, transfer tokens to `SubProfitItem`s' virtual addresses.

### Notes

</details>

<details>

  <summary><b>DistributeProfits</b></summary>

### Purpose

For anyone to add profits to a certain `ProfitItem` of specific period.

### Notes

</details>

<details>

  <summary><b>Profit</b></summary>

### Purpose

For a user to get all available profits from a certain `ProfitItem`.

### Notes

</details>