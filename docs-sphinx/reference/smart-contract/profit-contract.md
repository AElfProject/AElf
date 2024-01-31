# Profit Contract

## Overview

Profit Contract can be used to manage the lifecycle of a profit scheme (dividend plan), 
such as creating a profit scheme, maintaining the beneficiaries of profits, and distributing profits.

The manager of the profit scheme can adjust 
the amount of profits that the beneficiary can receive when releasing profit in the future, 
by maintaining the shares of each profit beneficiary.

At the same time, a profit scheme can also have sub profit schemes, 
which will exist as special profit beneficiaries. 
The sub profit scheme independently maintains its beneficiaries and profit distribution time.

In the aelf MainChain, 
we use the Profit Contract to manage the benefits of Block Producers and 
the revenue of voters participating in the node elections.

In this article, we will discuss:

- How to create a profit scheme
- How to manage shares of beneficiaries
- How to contribute and distribute profits
- Application of Profit Contract in AElf Economic System
- Profit Contract method explanation

## How to create a profit scheme

In the Profit Contract, the `Scheme` structure is used to store basic information of a profit scheme:

```
message Scheme {
    // The virtual address of the scheme.
    aelf.Address virtual_address = 1;
    // The total weight of the scheme.
    int64 total_shares = 2;
    // The manager of the scheme.
    aelf.Address manager = 3;
    // The current period.
    int64 current_period = 4;
    // Sub schemes information.
    repeated SchemeBeneficiaryShare sub_schemes = 5;
    // Whether you can directly remove the beneficiary.
    bool can_remove_beneficiary_directly = 6;
    // Period of profit distribution.
    int64 profit_receiving_due_period_count = 7;
    // Whether all the schemes balance will be distributed during distribution each period.
    bool is_release_all_balance_every_time_by_default = 8;
    // The is of the scheme.
    aelf.Hash scheme_id = 9;
    // Delay distribute period.
    int32 delay_distribute_period_count = 10;
    // Record the scheme's current total share for deferred distribution of benefits, period -> total shares.
    map<int64, int64> cached_delay_total_shares = 11;
    // The received token symbols.
    repeated string received_token_symbols = 12;
}
```

The field `scheme_id` is the unique identifier of a profit scheme.

And more importantly, `virtual_address` can be considered as the overall ledger address for a profit scheme.
If you want to know how much money is in the dividend pool of a profit scheme, 
you can obtain the token balance of the `virtual_address` through the `GetBalance` method of the MultiToken Contract.
The symbol of the token in the dividend pool is recorded in the `received_token_symbols` field.
Then it is obvious that a profit scheme's dividend pool can allow for the existence of multiple types of tokens.

Each beneficiary and sub profit scheme has an amount of **shares**, and if these shares are added, it is `totol_shares` field.
And when tokens in the dividend pool are released, 
beneficiaries and sub profit schemes receive a certain number of tokens based on their respective shares.

The input type of `CreateScheme` method is:

```
message CreateSchemeInput {
    // Period of profit distribution.
    int64 profit_receiving_due_period_count = 1;
    // Whether all the schemes balance will be distributed during distribution each period.
    bool is_release_all_balance_every_time_by_default = 2;
    // Delay distribute period.
    int32 delay_distribute_period_count = 3;
    // The manager of this scheme, the default is the creator.
    aelf.Address manager = 4;
    // Whether you can directly remove the beneficiary.
    bool can_remove_beneficiary_directly = 5;
    // Use to generate scheme id.
    aelf.Hash token = 6;
}
```

The following is the process of creating a profit scheme on the aelf TestNet using the **aelf-command** tool:

```
aelf-command send AElf.ContractNames.Profit CreateScheme
? Enter the the URI of an AElf node: https://aelf-test-node.aelf.io
? Enter a valid wallet address, if you don't have, create one by aelf-command create: 2WNUjzNPyd7dBwo9a
5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd
? Enter the password you typed when creating a wallet: ********
✔ Fetching contract successfully!

If you need to pass file contents as a parameter, you can enter the relative or absolute path of the file

Enter the params one by one, type `Enter` to skip optional param:
? Enter the required param <profitReceivingDuePeriodCount>: 0
? Enter the required param <isReleaseAllBalanceEveryTimeByDefault>: true
? Enter the required param <delayDistributePeriodCount>: 0
? Enter the required param <manager>:
? Enter the required param <canRemoveBeneficiaryDirectly>: true
? Enter the required param <token>: ELF
The params you entered is:
{
  "profitReceivingDuePeriodCount": 0,
  "isReleaseAllBalanceEveryTimeByDefault": true,
  "delayDistributePeriodCount": 0,
  "canRemoveBeneficiaryDirectly": true,
  "token": "ELF"
}
✔ Succeed!
AElf [Info]:
Result:
{
  "TransactionId": "ab97bd5a3d42e2f8ab3d2f3e02019fe232369380791e4c90180c54b318630f9d"
}
✔ Succeed!
```
- Each time the profit scheme distributes tokens from the dividend pool we call it a **period**. `profitReceivingDuePeriodCount` is used to limit how many periods of dividends a user can receive each time. If not set, it defaults to 10.
- If `isReleaseAllBalanceEveryTimeByDefault` is true, then every time the profit scheme manager distribute tokens, if the number of tokens distributed is not specified, all balance in `virtual_address` will be distributed by default.
- The dividends generated in the current period can be distributed in the future, use the `delayDistributePeriodCount` to set how many period will be delay distributed.
- If `canRemoveBeneficiaryDirectly` is true, the profit scheme manager can remove a beneficiary without considering he's shares are not expired.
- It is necessary to specify a default `token` for the profit scheme when creating it. However, it should be noted that this does not limit the distribution of this profit scheme to only one token.

After the creation, you can use the following command to get your `SchemeId` and `VirtualAddress`:

```
aelf-command event ab97bd5a3d42e2f8ab3d2f3e02019fe232369380791e4c90180c54b318630f9d
? Enter the the URI of an AElf node: https://aelf-test-node.aelf.io

[Info]:
The results returned by
Transaction: ab97bd5a3d42e2f8ab3d2f3e02019fe232369380791e4c90180c54b318630f9d is:
[
  {
    "Address": "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
    "Name": "TransactionFeeCharged",
    "Indexed": [
      "GiIKIMZijqxLIVIayGidcVc0r+o9XtJ5T1N80m4rQ/qfnd38"
    ],
    "NonIndexed": "CgNFTEYQtOqhrQM=",
    "Result": {
      "chargingAddress": "2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd",
      "symbol": "ELF",
      "amount": "900232500"
    }
  },
  {
    "Address": "2ZUgaDqWSh4aJ5s5Ker2tRczhJSNep4bVVfrRBRJTRQdMTbA5W",
    "Name": "SchemeCreated",
    "Indexed": [],
    "NonIndexed": "CiIKIEFwvVDodszlfIh14Vzp6K07Nduws7ZZY43hC3gl+ZtrEiIKIMZijqxLIVIayGidcVc0r+o9XtJ5T1N80m4rQ/qfnd38GAogASoiCiBhamBcQiHIK9XIinJ5+8/00jtJUQEKy7vuJKavwrY1mQ==",
    "Result": {
      "virtualAddress": "Vpb3LDUnkgrVetF1RADywdLbXQ5fY95VeLymgVsM7ti1XUyh4",
      "manager": "2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd",
      "profitReceivingDuePeriodCount": "10",
      "isReleaseAllBalanceEveryTimeByDefault": true,
      "schemeId": "616a605c4221c82bd5c88a7279fbcff4d23b4951010acbbbee24a6afc2b63599"
    }
  }
]
✔ Succeed!
```

As you can see from the event message:
- The SchemeId is `616a605c4221c82bd5c88a7279fbcff4d23b4951010acbbbee24a6afc2b63599`.
- The VirtualAddress is `Vpb3LDUnkgrVetF1RADywdLbXQ5fY95VeLymgVsM7ti1XUyh4`.

## How to manage shares of beneficiaries

There are two types of profits beneficiaries:

- Normal beneficiary, which means that someone can operate the account through a private key.
- Sub profit scheme, just like the profit scheme created above. 
And the sub profit scheme can be allocated a certain share of profits like the normal beneficiary.

If you're going to add a new normal beneficiary, you can send an `AddBeneficiary` transaction:

```
aelf-command send AElf.ContractNames.Profit AddBeneficiary
? Enter the the URI of an AElf node: https://aelf-test-node.aelf.io
? Enter a valid wallet address, if you don't have, create one by aelf-command create: 2WNUjzNPyd7dBwo9a5KG
56AXfPCdwmcydXv4kDyTyFZwTum5nd
? Enter the password you typed when creating a wallet: ********
✔ Fetching contract successfully!

If you need to pass file contents as a parameter, you can enter the relative or absolute path of the file

Enter the params one by one, type `Enter` to skip optional param:
? Enter the required param <schemeId>: 616a605c4221c82bd5c88a7279fbcff4d23b4951010acbbbee24a6afc2b63599
? Enter the required param <beneficiaryShare.beneficiary>: 2HeW7S9HZrbRJZeivMppUuUY3djhWdfVnP5zrDsz8wqq6hK
MfT
? Enter the required param <beneficiaryShare.shares>: 1
? Enter the required param <endPeriod>: 10
? Enter the required param <profitDetailId>:
The params you entered is:
{
  "schemeId": "616a605c4221c82bd5c88a7279fbcff4d23b4951010acbbbee24a6afc2b63599",
  "beneficiaryShare": {
    "beneficiary": "2HeW7S9HZrbRJZeivMppUuUY3djhWdfVnP5zrDsz8wqq6hKMfT",
    "shares": 1
  },
  "endPeriod": 10
}
✔ Succeed!
AElf [Info]:
Result:
{
  "TransactionId": "3afa8abf9560815ce43a8d585aa3e1a432711171a0ec2a002b7ba621dfe92dc0"
}
✔ Succeed!
```

Then the profit scheme will have the first beneficiary `2HeW7S9HZrbRJZeivMppUuUY3djhWdfVnP5zrDsz8wqq6hKMfT`, 
and the shares of this beneficiary is 1.

If you no longer add other beneficiaries, 
the next time the profit scheme is released, 
this account will receive all profits; 
If another beneficiary with a share of 1 is added, 
the two beneficiaries will share the profits equally.

Now if you use the `GetScheme` method to check your profit scheme:

```
aelf-command call AElf.ContractNames.Profit GetScheme
? Enter the the URI of an AElf node: https://aelf-test-node.aelf.io
? Enter a valid wallet address, if you don't have, create one by aelf-command create: 2WNUjzNPyd7dBwo9a5KG
56AXfPCdwmcydXv4kDyTyFZwTum5nd
? Enter the password you typed when creating a wallet: ********
✔ Fetching contract successfully!

If you need to pass file contents as a parameter, you can enter the relative or absolute path of the file

Enter the params one by one, type `Enter` to skip optional param:
? Enter the required param <value>: 616a605c4221c82bd5c88a7279fbcff4d23b4951010acbbbee24a6afc2b63599
The params you entered is:
"616a605c4221c82bd5c88a7279fbcff4d23b4951010acbbbee24a6afc2b63599"
✔ Calling method successfully!
AElf [Info]:
Result:
{
  "subSchemes": [],
  "cachedDelayTotalShares": {},
  "receivedTokenSymbols": [],
  "virtualAddress": "Vpb3LDUnkgrVetF1RADywdLbXQ5fY95VeLymgVsM7ti1XUyh4",
  "totalShares": "1",
  "manager": "2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd",
  "currentPeriod": "1",
  "canRemoveBeneficiaryDirectly": true,
  "profitReceivingDuePeriodCount": "10",
  "isReleaseAllBalanceEveryTimeByDefault": true,
  "schemeId": "616a605c4221c82bd5c88a7279fbcff4d23b4951010acbbbee24a6afc2b63599",
  "delayDistributePeriodCount": 0
}
✔ Succeed!
```

You can see the `totalShares` now is 1.

If you want to add multiple beneficiaries at once, you can use the `AddBeneficiaries` method.
Then these beneficiaries will share the same `endPeriod`.

And if you want to add sub profit schemes as a beneficiary, instead of providing the beneficiary address,
you need to provide the `subSchemeId` and `subSchemeShares`. 
No need to provide `endPeriod` in this case because the lasting period of sub profit schemes will be forever by default
until you remove it via a `RemoveSubScheme` transaction.

