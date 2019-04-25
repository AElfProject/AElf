# AElf Consensus Contract

## Actions

<details>

  <summary><b>InitialAElfConsensusContract</b></summary>

### Usage

Iniitalize this contract.

### Required

None.

### Optional

- `ElectionContractSystemName`. `AElf Consensus Contract` of AElf Mainchain need to use `Election Contract` to provide election information and release profits from `Treasury` item.

- `DaysEachTerm`. Will be `int.MaxValue` if not provided.

- `IsSideChain`. If true, DaysEachTerm will be `int.MaxValue`.

- `IsTermStayOne`. If true, DaysEachTerm will be `int.MaxValue`.

### Notes

- For AElf Mainchain or any test chain, `ElectionContractSystemName` and `DaysEachTerm` are required.

</details>

