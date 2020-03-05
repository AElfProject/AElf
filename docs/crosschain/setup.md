## Proposing a side-chain

Side-chains can be created in the AELF ecosystem to enable scalability. The proposer/creator of a new side chain will need to request the creation of the side-chain through the cross-chain contract on the main-chain. The request contains different fields that will determine the type of side-chain that will be created.

#### The request

```Proto
message SideChainCreationRequest {
    int64 indexing_price = 1; // initial index fee
    int64 locked_token_amount = 2;
    bool is_privilege_preserved = 3; // exclusive or shared
    string side_chain_token_symbol = 4;
    string side_chain_token_name = 5;
    sint64 side_chain_token_total_supply = 6;
    sint32 side_chain_token_decimals = 7;
    bool is_side_chain_token_burnable = 8;
    repeated SideChainTokenInitialIssue side_chain_token_initial_issue_list = 9;
    map<string, sint32> initial_resource_amount = 10; // when charging by time (exclusive side-chain) this must be set
    bool is_side_chain_token_profitable = 11;
}
```

#### Exclusive and shared 

Two types of side-chain's currently exist: **exclusive** or **shared**. To decide wether the side chain is **exclusive** or **shared**, the creation request must set the **is_privilege_preserved** flag to either true or false.

An **exclusive** side-chain is a type of dedicated side-chain (as opposed to shared) that allows developers to choose the transaction fee model and set the transaction fee price. The developer has exclusive use of this side-chain.  
Developers of an exclusive side-chain pay the producers for running it by paying CPU, RAM, DISK, NET resource tokens: this model is called *charge-by-time*. The amount he must share with the producers is set after creation of the chain. The **exclusive** side-chain is priced according to the time used. The unit price of the fee is determined through
negotiation between the production node and the developer. An **exclusive** side-chain can charge for execution in any way he wants.

A **shared** side-chain is a side-chain on which any developer can deploy a contract. This will be charged by resource usage and this is enabled by the fact that the contracts deployed on this side-chain should implement either ACS1 (users pay for the cost of executing his transaction) or ACS8 (the contract developer will pay).

See [Economic whitepaper - 4.3 Sidechain Developer Charging Model](https://aelf.io/gridcn/aelf_economic_system_whitepaper_en_v1.0.pdf?time=1) for more information.

#### Indexing fees

Side-chain developers who want to implement cross-chain transfers and cross-chain verification
need to have the main-chain index the side-chain's blocks. In order to use it, they need
to pay the block index fee. The unit used for the block index fee is ELF. The amount charged is
determined conjointly by the organization and the developer. The initial index fee is passed in as
a parameter when applying to create a side chain (**indexing_price**). The index fee amount can be adjusted
through a proposal. It will take effect when both the organization and the developer agree to the
adjusted plan.

After the side chain is successfully created, the deposited ELF (**locked_token_amount**) will be used to deduct the index
fee (the index fee amount is jointly determined by the production nodes). 

**Important**: both **exclusive** sidechain and **shared** sidechain should pay for index fee.






