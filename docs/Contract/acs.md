# AElf Contract Standard(ACS)

Description

## ACS0

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs0.proto)

Description

## ACS1

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs1.proto)

Description

## ACS2

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs2.proto)

Description

## ACS3

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs3.proto)

Description

## ACS4

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs4.proto)

Description

## ACS5

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs5.proto)

Description

## ACS6

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs6.proto)

Description

ACS6 is aiming at providing two standard interfaces of requesting and getting random numbers. It implies using commit-reveal schema during random number generation and verification, though other solutions can also expose their services via these two interfaces.
In the commit-reveal schema, the user needs to call `RequestRandomNumberInput ` on his initiative as well as provide a block height number as the minimum height to enable the query of random number. Then the contract implemented ACS6 need to return a hash value as the token for querying random number, and a negotiated block height to enable related query. When the block height reaches, the user can use that token, which is a hash value, call `GetRandomNumber` to query the random number.
The implementation of ACS6 in `AEDPoS Contract` shows how commit-reveal schema works.

## ACS7

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs7.proto)

Description

## ACS8

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs8.proto)

Description
