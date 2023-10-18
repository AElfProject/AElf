Smart contracts Overview
========================

This section provides the knowledge you need to write smart contracts, 
and it will take you approximately 40 minutes to complete the entire tutorial.
You can follow the instructions, along with the examples, to learn how to set up 
the development environment using C# and AElf-developer-tools. The instructions will guide 
you through the development, testing, and deployment/update of smart contracts.
There are two types of smart contract deployment/update: one that requires approval from 
a BP and the other that does not. You can find in the tutorials the scenarios in which each type is applicable.

The core of blockchain platforms can be viewed as a distributed
multi-tenant database that stores the status of all the smart contracts
deployed on it. Once deployed, each smart contract will have its unique
address. The address will be used to query the execution status of the 
contract and can serve as an identifier for status queries and updates.
The contract code defines the details of these and updates, to be
specific, how to check whether an account has permission to operate them
and how the operation is completed.